using PanoramicData.ChartMagic.Interfaces;
using PanoramicData.ChartMagic.Renderers;
using Svg;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PanoramicData.ChartMagic.Models
{
	public class Chart : IChartElement
	{
		public Chart()
		{
			ChartBackgroundArea = new(this, "Chart Background Area");
			ChartArea = new(ChartBackgroundArea, "Chart Area");
			Legends = new(ChartBackgroundArea);
			Series = new(ChartBackgroundArea);
			Titles = new(ChartBackgroundArea);
			Annotations = new(ChartBackgroundArea);
		}

		public ChartBackgroundArea ChartBackgroundArea { get; set; }

		public ChartArea ChartArea { get; }

		public SeriesCollection Series { get; }

		public LegendCollection Legends { get; }

		public TitleCollection Titles { get; }

		public AnnotationCollection Annotations { get; }

		public void SaveImage(Stream stream, ChartImageFormat chartImageFormat, bool debug = false)
		{
			if (chartImageFormat == ChartImageFormat.Svg)
			{
				new InternalSvgRenderer(debug)
				 .SaveImage(stream, this);
				return;
			}

			var tempFileInfo = new FileInfo(Path.GetTempFileName());
			var svgTempFileInfo = new FileInfo(tempFileInfo.FullName + ".svg");
			File.Move(tempFileInfo.FullName, svgTempFileInfo.FullName);
			try
			{
				using (var svgFileStream = new FileStream(svgTempFileInfo.FullName, FileMode.Create, FileAccess.Write))
				{
					new InternalSvgRenderer(debug)
						.SaveImage(svgFileStream, this);
					svgFileStream.Flush();
				}
				var svgDocument = SvgDocument.Open(svgTempFileInfo.FullName);
				svgDocument.ShapeRendering = SvgShapeRendering.Auto;

				// Is the output EMF (vector)?
				if (chartImageFormat == ChartImageFormat.Emf)
				{
					// Yes.
					using var bufferGraphics = Graphics.FromHwndInternal(IntPtr.Zero);
					using var metafile = new Metafile(stream, bufferGraphics.GetHdc());
					using var graphics = Graphics.FromImage(metafile);
					svgDocument.Draw(graphics);
					return;
				}
				// No. Output as a bitmap

				Bitmap bmp = svgDocument.Draw(
					(int)ChartBackgroundArea.Width,
					(int)ChartBackgroundArea.Height
					);
				switch (chartImageFormat)
				{
					case ChartImageFormat.Png:
						bmp.Save(stream, ImageFormat.Png);
						break;
					case ChartImageFormat.Jpeg:
						bmp.Save(stream, ImageFormat.Jpeg);
						break;
					case ChartImageFormat.Bmp:
						bmp.Save(stream, ImageFormat.Bmp);
						break;
					case ChartImageFormat.Tiff:
						bmp.Save(stream, ImageFormat.Tiff);
						break;
					case ChartImageFormat.Gif:
						bmp.Save(stream, ImageFormat.Gif);
						break;
					default:
						throw new NotSupportedException();
				}
			}
			finally
			{
				svgTempFileInfo.Delete();
			}
		}
	}
}
