namespace PanoramicData.ChartMagic.Models;

public class Chart : RootChartElement
{
	public Chart() : base()
	{
		ChartBackgroundArea = new(this, "Chart Background Area");
		ChartArea = new(ChartBackgroundArea, "Chart Area");
		Legends = new(ChartBackgroundArea, []);
		Series = new(ChartBackgroundArea, []);
		Titles = new(ChartBackgroundArea, []);
		Annotations = new(ChartBackgroundArea, []);
	}

	public ChartBackgroundArea ChartBackgroundArea { get; set; }

	public ChartArea ChartArea { get; }

	public SeriesCollection Series { get; }

	public LegendCollection Legends { get; }

	public TitleCollection Titles { get; }

	public AnnotationCollection Annotations { get; }

	public void SaveImage(Stream stream, ChartImageFormat chartImageFormat, int widthPixels, int heightPixels, bool debug = false)
	{
		if (chartImageFormat == ChartImageFormat.Svg)
		{
			new InternalSvgRenderer(widthPixels, heightPixels, debug)
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
				new InternalSvgRenderer(widthPixels, heightPixels, debug)
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

			var bmp = svgDocument.Draw(
				widthPixels,
				heightPixels
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
