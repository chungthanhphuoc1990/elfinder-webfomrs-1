using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace elFinder.Connector.Service
{
	public interface IVolume
	{
		string Id { get; set; }

		string Name { get; }

		Model.DirectoryModel GetDirectoryByHash( string directoryHash );
		Model.DirectoryModel GetRootDirectory();

		IEnumerable<Model.DirectoryModel> GetSubdirectoriesFlat( Model.DirectoryModel rootDirectory, int? maxDepth = null );

		IEnumerable<Model.FileModel> GetFiles( Model.DirectoryModel rootDirectory );

		string GetPathToRoot( Model.DirectoryModel startDir );

		string EncodePathToHash( string path, bool fromAbsolutePath = true );
		string DecodeHashToPath( string hash, bool toAbsolutePath = true );

		Model.DirectoryModel CreateDirectory( Model.DirectoryModel cwd, string name );
		Model.FileModel CreateFile( Model.DirectoryModel inDir, string name );

		Model.FileModel GetFileByHash( string fileHash );

		Model.FileModel RenameFile( Model.FileModel fileToChange, string newname );
		Model.DirectoryModel RenameDirectory( Model.DirectoryModel dirToChange, string newname );

		bool DeleteFile( Model.FileModel fileToRemove );
		bool DeleteDirectory( Model.DirectoryModel directoryToRemove );

		Model.FileModel[] SaveFiles( string targetDirHash, HttpFileCollection files );
	}
}
