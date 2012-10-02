using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace elFinder.Connector.Model
{
	[JsonObject]
	public class FileModel : ObjectModel
	{
		public FileModel( string name, string hash, long size, string parentHash, 
			DateTime dateModified, string volumeId, bool isReadable, bool isWritable, bool isLocked )
		{
			Name = name;
			Hash = hash;
			ParentHash = parentHash;
			ModifiedTimeStamp = getTS( dateModified );
			VolumeID = volumeId;
			IsReadable = isReadable.ToInt();
			IsWriteable = isWritable.ToInt();
			IsLocked = isLocked.ToInt();

			Mime = MimeTypes.GetContentType( name );
			Size = size;
		}
	}
}
