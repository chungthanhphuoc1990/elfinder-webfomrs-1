using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace elFinder.Connector.Service
{
	public interface IImageEditorService
	{
		string CreateThumbnail( string sourceImagePath, string destThumbsDir, string thumbnailName, Size thumbSize, bool restrictWidth );
		IEnumerable<string> SupportedExtensions { get; }
		bool CanGenerateThumbnail( string filePath );
	}
}
