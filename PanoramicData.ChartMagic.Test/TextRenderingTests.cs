using PanoramicData.ChartMagic.Renderers;
using SkiaSharp;
using System.Reflection;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Guards issue #60: charts that rendered with no text at all.
/// </summary>
/// <remarks>
/// The existing raster tests look for many colours and content across the canvas. A chart missing
/// every label still satisfies both - the bars, lines and legend swatches are all still there - so
/// the estate rendered label-less charts for some time without a single test noticing.
///
/// These assert on text specifically, and they assert it the only way that is honest here: by
/// rendering the same chart with and without a caption and requiring the pixels to differ. An
/// assertion that merely counts colours would pass on a chart with no glyphs in it.
///
/// This matters most on Linux. Consumers commonly reference
/// <c>SkiaSharp.NativeAssets.Linux.NoDependencies</c>, whose libSkiaSharp is built without
/// fontconfig and can therefore enumerate no system fonts at all. The test project references that
/// same package (issue #27), so CI runs on exactly the configuration that failed.
/// </remarks>
public class TextRenderingTests : RenderTest
{
	/// <summary>
	/// The font is actually in the assembly.
	/// </summary>
	/// <remarks>
	/// First, because every other assertion here depends on it and a missing resource would
	/// otherwise surface as a puzzling rendering difference rather than as a missing file.
	/// </remarks>
	[Fact]
	public void EmbeddedFont_IsPresentInTheAssembly()
	{
		var names = typeof(Chart).GetTypeInfo().Assembly.GetManifestResourceNames();

		names.Should().Contain(
			name => name.EndsWith("LiberationSans-Regular.ttf", StringComparison.Ordinal),
			"the embedded font is what makes text render on hosts with no font manager");
	}

	/// <summary>
	/// The embedded font loads as a usable typeface.
	/// </summary>
	/// <remarks>
	/// Present but corrupt would fail in exactly the same silent way as absent.
	/// </remarks>
	[Fact]
	public void EmbeddedFont_LoadsAndHasGlyphs()
	{
		var typeface = new EmbeddedTypefaceProvider()
			.FromFamilyName("anything", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

		typeface.Should().NotBeNull("the provider must return a typeface on every host");
		typeface!.FamilyName.Should().Be(EmbeddedTypefaceProvider.FamilyName);
		typeface.GlyphCount.Should().BeGreaterThan(0, "a typeface with no glyphs draws nothing");
	}

	/// <summary>
	/// Adding an axis title changes the rendered pixels.
	/// </summary>
	/// <remarks>
	/// The assertion that would have caught #60. If no glyphs are drawn, the two renders are
	/// byte-identical and this fails - which is precisely what happened on Linux before the fix.
	///
	/// Comparing two renders rather than looking for dark pixels in a region keeps this robust
	/// against layout changes: it does not care where the text lands, only that drawing it made a
	/// difference.
	/// </remarks>
	[Fact]
	public void AddingAnAxisTitle_ChangesThePixels()
	{
		var withoutTitle = RenderWithTitle(titleText: null);
		var withTitle = RenderWithTitle(titleText: "Rendered title");

		withTitle.Should().NotEqual(
			withoutTitle,
			"a chart with an axis title must not be pixel-identical to one without - if it is, no glyphs were drawn");
	}

	/// <summary>
	/// A longer axis title changes more pixels than a shorter one.
	/// </summary>
	/// <remarks>
	/// Distinguishes real glyphs from any single artefact that merely correlates with the title
	/// being set - a background box, say, which would differ from the untitled render identically
	/// whatever the text said.
	/// </remarks>
	[Fact]
	public void ALongerAxisTitle_DiffersMoreThanAShorterOne()
	{
		var baseline = RenderWithTitle(titleText: null);

		var shortDifference = DifferingPixels(baseline, RenderWithTitle(titleText: "I"));
		var longDifference = DifferingPixels(baseline, RenderWithTitle(titleText: "MMMMMMMMMMMMMMMMMMMM"));

		shortDifference.Should().BeGreaterThan(0, "even one character must draw something");
		longDifference.Should().BeGreaterThan(
			shortDifference,
			"twenty wide characters must mark more pixels than one narrow one, which is only true "
				+ "if the glyphs themselves are being drawn");
	}

	/// <summary>
	/// The SVG names a font, so a browser lays it out as we do.
	/// </summary>
	/// <remarks>
	/// SVG is a public output format. Our own raster path resolves the typeface itself and ignores
	/// this attribute, but anything else opening the file would otherwise choose its own default
	/// and disagree with the PNG of the same chart.
	/// </remarks>
	[Fact]
	public void Svg_NamesAFont()
	{
		var fileInfo = GetTempFileName(ChartImageFormat.Svg);
		try
		{
			var specification = WithAxisTitle("Rendered title");
			SaveFile(specification, fileInfo);

			var svg = File.ReadAllText(fileInfo.FullName);

			svg.Should().Contain("font-family", "text in an SVG must say what to draw itself with");
			svg.Should().Contain(
				EmbeddedTypefaceProvider.FamilyName,
				"the SVG should lead with the same face the raster path uses");
		}
		finally
		{
			fileInfo.Refresh();
			if (fileInfo.Exists)
			{
				fileInfo.Delete();
			}
		}
	}

	private ChartSpecification WithAxisTitle(string? titleText)
	{
		var specification = BasicChartSpecification;
		specification.XAxisTitle = titleText;
		return specification;
	}

	/// <summary>
	/// The PNG of the fixture chart, captioned or not.
	/// </summary>
	private byte[] RenderWithTitle(string? titleText) => RenderToPngBytes(WithAxisTitle(titleText));

	private static int DifferingPixels(byte[] firstPng, byte[] secondPng)
	{
		using var first = SKBitmap.Decode(firstPng);
		using var second = SKBitmap.Decode(secondPng);

		first.Should().NotBeNull();
		second.Should().NotBeNull();
		second!.Width.Should().Be(first!.Width);
		second.Height.Should().Be(first.Height);

		var differing = 0;

		for (var x = 0; x < first.Width; x++)
		{
			for (var y = 0; y < first.Height; y++)
			{
				if (first.GetPixel(x, y) != second.GetPixel(x, y))
				{
					differing++;
				}
			}
		}

		return differing;
	}
}
