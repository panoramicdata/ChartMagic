using SkiaSharp;
using PanoramicData.ChartMagic.Renderers;
using Svg.Skia;

namespace PanoramicData.ChartMagic.Models;

public class Chart : RootChartElement
{
	public Chart()
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

	public void SaveImage(Stream stream, ChartImageFormat format, int width, int height, bool debug)
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

		// Issue #60: draw text with the font carried in this assembly rather than one the host
		// happens to have. Inserted ahead of the system providers so the choice is ours on every
		// platform; see EmbeddedTypefaceProvider for why the obvious alternatives do not work.
		skSvg.Settings.TypefaceProviders?.Insert(0, new EmbeddedTypefaceProvider());

		skSvg.Load(svgStream);
		if (skSvg.Picture is null)
		{
			throw new InvalidOperationException("SVG picture is null.");
		}

		// Issue #27: draw the picture directly rather than through the scaling overload.
		//
		// The previous call passed the requested pixel width and height into an overload whose
		// corresponding parameters are scale factors, so the picture was magnified by a factor
		// of the canvas size. One element - whichever background rectangle sat at the origin -
		// was blown up to fill the frame and everything else was pushed off canvas, which is
		// why raster output was a single flat colour while the SVG was correct.
		//
		// The SVG now carries a viewBox matching the requested size, so the picture is already
		// in the right coordinate space and needs no scaling here.
		canvas.DrawPicture(skSvg.Picture);

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

		// Encode returns null rather than throwing for a format Skia cannot write, and BMP, GIF
		// and TIFF are all in that category - it writes PNG, JPEG and WEBP only. Without this
		// check the next line threw a NullReferenceException from inside SaveImage, which tells
		// the caller nothing about which format was refused or why.
		if (encoded is null)
		{
			throw new NotSupportedException(
				$"{format} cannot be encoded: the underlying imaging library writes PNG, JPEG "
				+ "and WEBP only. Render PNG, or SVG for vector output.");
		}

		encoded.SaveTo(stream);
	}

}
