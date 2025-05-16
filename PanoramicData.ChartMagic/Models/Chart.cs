using SkiaSharp;
using Svg.Skia;

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

	// If there is no debug parameter
	public void SaveImage(Stream stream, ChartImageFormat chartImageFormat, int widthPixels, int heightPixels)
		=> SaveImage(stream, chartImageFormat, widthPixels, heightPixels, false);

	public void SaveImage(Stream stream, ChartImageFormat format, int width, int height, bool debug = false)
	{
		if (format == ChartImageFormat.Svg)
		{
			new InternalSvgRenderer(width, height, debug)
				.SaveImage(stream, this);
			return;
		}

		using var svgStream = new MemoryStream();
		new InternalSvgRenderer(width, height, debug)
			.SaveImage(svgStream, this);
		svgStream.Position = 0;

		using var surface = SKSurface.Create(new SKImageInfo(width, height));
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.White);

		using var skSvg = new SKSvg();
		skSvg.Load(svgStream);
		if (skSvg.Picture is null)
		{
			throw new InvalidOperationException("SVG picture is null.");
		}

		skSvg.Picture.Draw(SKColors.Transparent, width, height, canvas);

		using var image = surface.Snapshot();

		var skFormat = format switch
		{
			ChartImageFormat.Png => SKEncodedImageFormat.Png,
			ChartImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
			ChartImageFormat.Bmp => SKEncodedImageFormat.Bmp,
			ChartImageFormat.Gif => SKEncodedImageFormat.Gif,
			ChartImageFormat.Tiff => throw new NotSupportedException("TIFF is not supported."),
			ChartImageFormat.Emf => throw new NotSupportedException("EMF is Windows-only."),
			_ => throw new NotSupportedException($"Unsupported format: {format}")
		};

		using var encoded = image.Encode(skFormat, quality: 100);
		encoded.SaveTo(stream);
	}

}
