using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

namespace elFinder.Connector.Service
{
	public class LocalFileSystemVolume : IVolume
	{
		private readonly Config.IConnectorConfig _config;
		private readonly ICryptoService _cryptoService;

		public LocalFileSystemVolume( Config.IConnectorConfig config, ICryptoService cryptoService )
		{
			_config = config;
			_cryptoService = cryptoService;
		}

		#region IVolume Members

		public string Id
		{
			get;
			set;
		}

		public string Name
		{
			get { return "LocalFileSystem"; }
		}

		public bool IsHashFromThisVolume( string hashToCheck )
		{
			if( string.IsNullOrWhiteSpace( hashToCheck ) )
				return false;
			return hashToCheck.StartsWith( Id, StringComparison.OrdinalIgnoreCase );
		}

		private DirectoryInfo getValidDirectoryInfo( string absolutePath )
		{
			DirectoryInfo di = new DirectoryInfo( absolutePath );
			if( !isValidDirectoryInfo( di ) )
				return null;
			return di;
		}

		private FileInfo getValidFileInfo( string absolutePath )
		{
			FileInfo fi = new FileInfo( absolutePath );
			if( !isValidFileInfo( fi ) )
				return null;
			return fi;
		}

		private bool isValidDirectoryInfo( DirectoryInfo di )
		{
			// first check if this directory isn't higher than our root
			if( di.FullName.Length < _config.LocalFSRootDirectoryPath.Length || !di.FullName.StartsWith( _config.LocalFSRootDirectoryPath, StringComparison.OrdinalIgnoreCase ) )
				return false;
			return ( di != null && di.Exists && di.Attributes.HasFlag( FileAttributes.Directory )
				&& !di.Attributes.HasFlag( FileAttributes.Hidden )
				&& !di.Attributes.HasFlag( FileAttributes.System ) ); // dont want hidden directories or files
		}

		private bool isValidFileInfo( FileInfo fi )
		{
			return ( fi != null && fi.Exists
				&& !fi.Attributes.HasFlag( FileAttributes.Hidden )
				&& !fi.Attributes.HasFlag( FileAttributes.System ) ); // dont want hidden directories or files
		}

		private Model.DirectoryModel createDirectoryModel( DirectoryInfo di, string directoryHash, string parentHash )
		{
			// check if path is root path - if yes then we need to change name
			string dirName = di.Name;
			if( di.FullName == _config.LocalFSRootDirectoryPath )
				dirName = _config.RootDirectoryName;
			return new Model.DirectoryModel( dirName, directoryHash, parentHash,
				di.GetDirectories().Length > 0, di.LastWriteTime, Id, true,
				!di.Attributes.HasFlag( FileAttributes.ReadOnly ), false );
		}

		private Model.FileModel createFileModel( FileInfo fi, string parentHash )
		{
			return new Model.FileModel( fi.Name, EncodePathToHash( fi.FullName ),
						fi.Length, parentHash, fi.LastWriteTime, Id,
						true, !fi.IsReadOnly, false );
		}

		public Model.DirectoryModel GetDirectoryByHash( string directoryHash )
		{
			// decrypt to get real path
			// check if passed hash starts with our id
			if( !IsHashFromThisVolume( directoryHash ) )
				return null;
			string absolutePath = DecodeHashToPath( directoryHash, true );
			// finally we can get some info
			DirectoryInfo di = getValidDirectoryInfo( absolutePath );
			if( di == null )
				return null; // dont want hidden directories

			string parentHash = null;
			if( isValidDirectoryInfo( di.Parent ) )
			{
				// check if parent path ends with separator - it is required
				string parentPath = di.Parent.FullName;
				if( parentPath.Last() != Path.DirectorySeparatorChar )
					parentPath += Path.DirectorySeparatorChar;
				parentHash = EncodePathToHash( parentPath );
			}

			return createDirectoryModel( di, directoryHash, parentHash );
		}

		public Model.FileModel GetFileByHash( string fileHash )
		{
			if( !IsHashFromThisVolume( fileHash ) )
				return null;

			string absolutePath = DecodeHashToPath( fileHash, true );
			var fi = getValidFileInfo( absolutePath );
			if( fi == null )
				return null;
			// validate directory also
			var fileDirectory = fi.Directory;
			if( !isValidDirectoryInfo( fileDirectory ) )
				return null;

			string parentPath = fileDirectory.FullName;
			if( parentPath.Last() != Path.DirectorySeparatorChar )
				parentPath += Path.DirectorySeparatorChar;

			return createFileModel( fi, EncodePathToHash( parentPath ) );
		}

		public Model.DirectoryModel GetRootDirectory()
		{
			string absolutePath = _config.LocalFSRootDirectoryPath;
			DirectoryInfo di = getValidDirectoryInfo( absolutePath );
			if( di == null )
				return null;

			return createDirectoryModel( di, EncodePathToHash( di.FullName ), null );
		}

		private void getSubDirs( string rootDir, string rootDirHash, List<Model.DirectoryModel> subDirs, 
			int maxTreeLevel, int level )
		{
			if( level > maxTreeLevel )
				return;

			string[] dirs = Directory.GetDirectories( rootDir );
			foreach( var dir in dirs )
			{
				DirectoryInfo di = getValidDirectoryInfo( dir );
				if( di == null )
					continue;
				string hash = EncodePathToHash( di.FullName );

				subDirs.Add( createDirectoryModel( di, hash, rootDirHash ) );
				getSubDirs( di.FullName, hash, subDirs, maxTreeLevel, level + 1 );
			}
		}

		public IEnumerable<Model.DirectoryModel> GetSubdirectoriesFlat( Model.DirectoryModel rootDirectory, int? maxDepth = null )
		{
			if( rootDirectory == null )
				return new List<Model.DirectoryModel>();

			string dirPath = DecodeHashToPath( rootDirectory.Hash );
			List<Model.DirectoryModel> subDirs = new List<Model.DirectoryModel>();
			try
			{
				getSubDirs( dirPath, rootDirectory.Hash, subDirs, maxDepth ?? _config.MaxTreeLevel, 0 );
			}
			catch
			{
				return new List<Model.DirectoryModel>();
			}
			return subDirs;
		}

		public IEnumerable<Model.FileModel> GetFiles( Model.DirectoryModel rootDirectory )
		{
			if( rootDirectory == null )
				return new List<Model.FileModel>();

			string dirPath = DecodeHashToPath( rootDirectory.Hash );
			string[] files = Directory.GetFiles( dirPath );
			var filesModels = files.Select( x =>
				{
					var fi = getValidFileInfo( x );
					if( fi == null )
						return null;
					return createFileModel( fi, rootDirectory.Hash );
				} ).Where( x => x != null );
			return filesModels;
		}

		public string GetPathToRoot( Model.DirectoryModel startDir )
		{
			StringBuilder sb = new StringBuilder( startDir.Name );
			var rootDir = GetRootDirectory();
			var currDir = startDir;
			while( currDir != null && currDir.Hash != rootDir.Hash )
			{
				if( currDir.ParentHash == null )
					break;
				var parentDir = GetDirectoryByHash( currDir.ParentHash );
				if( parentDir == null )
					break;
				// add our root dir
				sb.Insert( 0, Path.DirectorySeparatorChar + parentDir.Name + Path.DirectorySeparatorChar );
				currDir = parentDir;
			}
			// trim separator from begining
			if( sb[ 0 ] == Path.DirectorySeparatorChar )
				sb.Remove( 0, 1 );
			return sb.ToString();
		}

		public Model.DirectoryModel CreateDirectory( Model.DirectoryModel inDir, string name )
		{
			if( inDir == null )
				return null;
			// compose final path of new directory
			string path = DecodeHashToPath( inDir.Hash );
			if( path.Last() != Path.DirectorySeparatorChar )
				path += Path.DirectorySeparatorChar;
			path += name;
			// check if directory exists
			if( Directory.Exists( path ) )
				return null;

			try
			{
				var createdDirectory = Directory.CreateDirectory( path );
				return createDirectoryModel( createdDirectory, EncodePathToHash( path ), inDir.Hash );
			}
			catch
			{
				return null;
			}
		}

		public Model.FileModel CreateFile( Model.DirectoryModel inDir, string name )
		{
			if( inDir == null )
				return null;
			// compose final path of new directory
			string path = DecodeHashToPath( inDir.Hash );
			if( path.Last() != Path.DirectorySeparatorChar )
				path += Path.DirectorySeparatorChar;
			path += name;
			// check if file exists
			if( File.Exists( path ) )
				return null;

			try
			{
				using( var createdFile = File.Create( path ) ) { }
				FileInfo fi = new FileInfo( path );
				return createFileModel( fi, inDir.Hash );
			}
			catch
			{
				return null;
			}
		}

		public Model.FileModel RenameFile( Model.FileModel fileToChange, string newname )
		{
			if( fileToChange == null )
				return null;

			string path = DecodeHashToPath( fileToChange.Hash );
			FileInfo fi = new FileInfo( path );

			string parentDir = fi.Directory.FullName;
			if( parentDir.Last() != Path.DirectorySeparatorChar )
				parentDir += Path.DirectorySeparatorChar;
			string newPath = parentDir + newname;

			if( File.Exists( newPath ) )
				return null;
			try
			{
				File.Move( path, newPath );
				FileInfo newFileInfo = new FileInfo( newPath );
				return createFileModel( newFileInfo, EncodePathToHash( parentDir ) );
			}
			catch
			{
				return null;
			}
		}

		public Model.DirectoryModel RenameDirectory( Model.DirectoryModel dirToChange, string newname )
		{
			if( dirToChange == null )
				return null;

			string path = DecodeHashToPath( dirToChange.Hash );
			DirectoryInfo di = new DirectoryInfo( path );
			if( di.Parent == null )
				return null;

			string parentDir = di.Parent.FullName;
			if( parentDir.Last() != Path.DirectorySeparatorChar )
				parentDir += Path.DirectorySeparatorChar;
			string newPath = parentDir + newname;

			if( Directory.Exists( newPath ) )
				return null;

			try
			{
				Directory.Move( path, newPath );
				DirectoryInfo newDirInfo = new DirectoryInfo( newPath );
				return createDirectoryModel( newDirInfo, EncodePathToHash( newDirInfo.FullName ),
					EncodePathToHash( parentDir ) );
			}
			catch
			{
				return null;
			}
		}

		public bool DeleteFile( Model.FileModel fileToDelete )
		{
			if( fileToDelete == null )
				return false;

			string path = DecodeHashToPath( fileToDelete.Hash );
			if( !File.Exists( path ) )
				return false;

			try
			{
				File.Delete( path );
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool DeleteDirectory( Model.DirectoryModel directoryToDelete )
		{
			if( directoryToDelete == null )
				return false;

			string path = DecodeHashToPath( directoryToDelete.Hash );
			if( !Directory.Exists( path ) )
				return false;

			try
			{
				Directory.Delete( path );
				return true;
			}
			catch
			{
				return false;
			}
		}

		public Model.FileModel[] SaveFiles( string targetDirHash, HttpFileCollection files )
		{
			if( files == null )
				return new Model.FileModel[ 0 ];

			string targetPath = DecodeHashToPath( targetDirHash );
			if( !Directory.Exists( targetPath ) )
				return new Model.FileModel[ 0 ];

			if( targetPath.Last() != Path.DirectorySeparatorChar )
				targetPath += Path.DirectorySeparatorChar;

			IList<Model.FileModel> added = new List<Model.FileModel>();
			for( int i = 0; i < files.Count; ++i )
			{
				HttpPostedFile file = files[ i ];
				string filePath = targetPath + file.FileName;
				try
				{
					file.SaveAs( filePath );
					FileInfo fi = new FileInfo( filePath );
					added.Add( createFileModel( fi, targetDirHash ) );
				}
				catch
				{
					continue;
				}
			}
			return added.ToArray();
		}

		public string EncodePathToHash( string path, bool fromAbsolutePath = true )
		{
			string relativePath = path;
			if( fromAbsolutePath )
			{
				if( path.Length < _config.LocalFSRootDirectoryPath.Length )
					throw new InvalidOperationException( "Cannot convert absolute path: " 
						+ path + " to relative to: " + _config.LocalFSRootDirectoryPath );
				// convert from absolute to relative
				relativePath = path.Substring( _config.LocalFSRootDirectoryPath.Length );
				if( string.IsNullOrWhiteSpace( relativePath ) )
					relativePath = "";
			}
			relativePath = relativePath.TrimEnd( '\\' );
			string encodedPath = _cryptoService.Encode( relativePath );
			// prepend with id
			return Id + encodedPath;
		}

		public string DecodeHashToPath( string hash, bool toAbsolutePath = true )
		{
			// remove volume id from the beginig
			string directoryHash = hash.Substring( Id.Length );
			// decode hash to real directory name
			string directoryRelativePath = _cryptoService.Decode( directoryHash );
			if( !toAbsolutePath )
				return directoryRelativePath;
			// now get absolute path
			string absolutePath = _config.LocalFSRootDirectoryPath;
			if( !string.IsNullOrWhiteSpace(directoryRelativePath) 
				&& directoryRelativePath[ 0 ] != Path.DirectorySeparatorChar 
				&& absolutePath.Last() != Path.DirectorySeparatorChar )
				absolutePath += Path.DirectorySeparatorChar;
			if( absolutePath.Last() == Path.DirectorySeparatorChar && directoryRelativePath[ 0 ] == Path.DirectorySeparatorChar )
				// need to remove one of the separators
				directoryRelativePath = directoryRelativePath.Substring( 1 );
			absolutePath += directoryRelativePath;
			return absolutePath;
		}

		#endregion
	}
}
