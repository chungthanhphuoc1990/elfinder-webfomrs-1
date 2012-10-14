using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace elFinder.Connector.Service
{
	public class DefaultImageEditorService : IImageEditorService
	{
		private static object _mutex = new object();
		private IList<string> _supportedExtensions;

		#region IThumbnailerService Members

		public IEnumerable<string> SupportedExtensions
		{
			get
			{
				if( _supportedExtensions == null )
				{
					lock( _mutex )
					{
						// cache supported extensions
						if( _supportedExtensions == null )
							_supportedExtensions = ImageCodecInfo.GetImageEncoders().SelectMany(
								x => x.FilenameExtension.Replace( "*.", string.Empty ).ToLowerInvariant().Split( ';' ) ).ToList();
					}
				}
				return _supportedExtensions;
			}
		}

		public bool CanGenerateThumbnail( string filePath )
		{
			string fileExt = Path.GetExtension( filePath ).ToLowerInvariant().Trim( '.' );
			return SupportedExtensions.Contains( fileExt );
		}

		//TODO handle error logging because exceptions might be tricky to find otherwise
		public string CreateThumbnail( string sourceImagePath, string destThumbsDir, string thumbFileName,
			System.Drawing.Size thumbSize, bool restrictWidth )
		{
			try
			{
				string fileExt = Path.GetExtension( sourceImagePath ).ToLowerInvariant().Trim( '.' );

				if( destThumbsDir.Last() != Path.DirectorySeparatorChar )
					destThumbsDir += Path.DirectorySeparatorChar;

				string outputFileName = thumbFileName + "." + fileExt;
				string outputPath = destThumbsDir + outputFileName;

				if( File.Exists( outputPath ) )
					return null;

				using( Bitmap inputImage = new Bitmap( sourceImagePath ) )
				{
					var encoder = ImageCodecInfo.GetImageEncoders().First( info => 
						info.FilenameExtension.IndexOf( fileExt, StringComparison.OrdinalIgnoreCase ) > -1 );
					EncoderParameters encParams = new EncoderParameters( 1 );
					encParams.Param[ 0 ] = new EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 90L );

					try
					{
						if( restrictWidth )
						{
							float scale = thumbSize.Width / (float)inputImage.Width;
							Size outSize = new Size( thumbSize.Width, (int)Math.Floor( inputImage.Height * scale ) );
							using( Bitmap outBmp = new Bitmap( outSize.Width, outSize.Height ) )
							{
								using( Graphics g = Graphics.FromImage( outBmp ) )
								{
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
									g.DrawImage( inputImage, 0, 0, outSize.Width, outSize.Height );
								}
								outBmp.Save( outputPath, encoder, encParams );
							}
						}
						else
						{
							// first rescale image to aspect ratio
							float wFactor = inputImage.Width / (float)thumbSize.Width;
							float hFactor = inputImage.Height / (float)thumbSize.Height;
							float minFactor = Math.Min( wFactor, hFactor );
							Size tempSize = new Size( (int)Math.Round( thumbSize.Width * minFactor ),
										(int)Math.Round( thumbSize.Height * minFactor ) );

							Rectangle clipRectangle = new Rectangle(
										( inputImage.Width - tempSize.Width ) / 2,
									   ( inputImage.Height - tempSize.Height ) / 2,
									   tempSize.Width, tempSize.Height );

							using( Bitmap tempBmp = new Bitmap( tempSize.Width, tempSize.Height ) )
							{
								// clip image
								tempBmp.SetResolution( inputImage.HorizontalResolution, inputImage.VerticalResolution );
								using( Graphics g = Graphics.FromImage( tempBmp ) )
								{
									g.DrawImage( inputImage, 0, 0, clipRectangle, GraphicsUnit.Pixel );
								}
								// resize bitmap
								using( Bitmap resBitmap = new Bitmap( thumbSize.Width, thumbSize.Height ) )
								{
									using( Graphics g = Graphics.FromImage( resBitmap ) )
									{
										g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
										g.DrawImage( tempBmp, 0, 0, thumbSize.Width, thumbSize.Height );
									}
									resBitmap.Save( outputPath, encoder, encParams );
								}
							}
						}
					}
					catch
					{
						return null;
					}

				}
				return outputFileName;
			}
			catch
			{
				return null;
			}
		}

		#endregion
	}
}
