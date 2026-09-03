using System.Drawing;
using static PanoramicData.ChartMagic.Test.Support.ChartFixtures;
using static PanoramicData.ChartMagic.Test.Support.RenderedChart;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Where things land on the canvas: category spacing, nested group transforms, area fills and
/// the markers of issue #30.
/// </summary>
public class PlotLayoutTests
{
	/// <summary>
	/// Categories are spaced by dividing the axis into one more interval than there are
	/// categories, with a whole interval of padding at each end.
	/// </summary>
	/// <remarks>
	/// Where the categories sit, not a rounding detail, and measured from the renderer this
	/// matches rather than chosen: over an inner plot 488 pixels wide with seven categories it
	/// spaced them 61 pixels apart starting 61 in - 488 / 8, where dividing by seven gives 70.
	/// Asserted as a ratio of the plot width so it holds at any size.
	/// </remarks>
	[Theory]
	[InlineData(SeriesChartType.Column)]
	[InlineData(SeriesChartType.Line)]
	public void Categories_AreSpacedByOneMoreIntervalThanThereAreCategories(SeriesChartType chartType)
	{
		var series = GroupById(Render(ColumnChart(chartType, 1)), "series0");

		// A column is positioned by its left edge and centred on its category, so the centre has
		// to be reconstructed; a line passes through the centres already, so they are read off
		// the vertices of its path.
		var centres = chartType == SeriesChartType.Column
			? [.. Elements(series, "rect")
				.Select(r => Number(r, "x") + (Number(r, "width") / 2))
				.OrderBy(x => x)]
			: PathVertexXValues(Elements(series, "path")[0]);

		centres.Should().HaveCount(Categories.Length);

		// Four categories divide the axis into five intervals, so the first centre is one fifth
		// of the way across and each is one fifth further on.
		var interval = centres[1] - centres[0];
		var expected = centres[^1] / Categories.Length;

		interval.Should().BeApproximately(
			expected,
			1.0,
			"the spacing between categories and the padding before the first are the same interval");

		centres[0].Should().BeApproximately(
			interval,
			1.0,
			"a whole interval of padding precedes the first category, not half of one");
	}

	/// <summary>
	/// An offset chart area moves the plot by that offset, not by twice it.
	/// </summary>
	/// <remarks>
	/// SVG transforms compound, so a group nested in a translated group is already moved by its
	/// parent. Translating it by its absolute position too moved it twice - invisible while every
	/// parent sat at the origin, which is the common case, and glaring the moment the chart area
	/// was offset to make room for a legend on the left: the last category fell off the canvas.
	///
	/// Asserted on the composed translation rather than on a drawn column, because that is where
	/// the error was - the columns were correctly placed within a plot that was in the wrong place.
	/// </remarks>
	[Fact]
	public void OffsetChartArea_DoesNotCompoundWithTheInnerPlotPosition()
	{
		const double ChartAreaLeftPercent = 20;
		const double ChartAreaWidthPercent = 80;

		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.ChartAreaXPositionPercent = ChartAreaLeftPercent;
		specification.ChartAreaWidthPercent = ChartAreaWidthPercent;

		var document = Render(specification);

		// What the viewer sees is the sum of the transforms down the tree.
		var composed = TranslationX(GroupById(document, "chartArea"))
			+ TranslationX(GroupById(document, "innerPlot"));

		// The inner plot sits at its own percentage of the chart area, offset by where the chart
		// area starts.
		var expected = Width * (ChartAreaLeftPercent
			+ (specification.InnerPlotXPositionPercent * ChartAreaWidthPercent / 100)) / 100;

		composed.Should().BeApproximately(
			expected,
			0.5,
			"the chart area offset should be applied once, by the chart area group");
	}

	[Fact]
	public void TranslucentFill_KeepsItsBorderOpaque()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.ChartBackgroundColor = Color.FromArgb(0x33, 0x77, 0x77, 0x77);
		specification.ChartBorderColor = Color.Black;

		var background = GroupById(Render(specification), "chartBackgroundArea");
		var style = Elements(background, "rect")[0].Attribute("style")!.Value;

		// Split into declarations rather than matching substrings: "fill-opacity" contains
		// "opacity", so a substring check cannot tell the two apart.
		var declarations = style.Split(';');

		declarations.Should().Contain("fill-opacity:0.20");
		declarations.Should().NotContain(
			"opacity:0.20",
			"issue #35: element opacity faded the border along with the fill");
		declarations.Should().Contain("stroke:#000000");
	}

	/// <summary>
	/// An area fill hangs below its own line, not from the corners of the plot.
	/// </summary>
	/// <remarks>
	/// The fill used to start at the bottom-left corner of the plot and finish at the
	/// bottom-right, which drew a diagonal ramp up to the first point and another down from the
	/// last - inventing data on either side of the series. With a whole category interval of
	/// padding at each end of the axis, those ramps were about a sixth of the chart wide, and
	/// obvious next to the reference render, whose fill drops vertically at the first and last
	/// points.
	/// </remarks>
	[Fact]
	public void AreaFill_StartsAndEndsUnderTheData()
	{
		var series = GroupById(Render(ColumnChart(SeriesChartType.Area, 1)), "series0");

		// Two paths: the outline, and the filled area below it. The fill is the closed one.
		var paths = Elements(series, "path");
		var vertices = PathVertexXValues(paths.Single(p => p.Attribute("d")!.Value.EndsWith('Z')));
		var lineVertices = PathVertexXValues(paths.Single(p => !p.Attribute("d")!.Value.EndsWith('Z')));

		// The fill spans exactly the same range of X as the line it belongs to.
		vertices.Min().Should().BeApproximately(
			lineVertices.Min(),
			0.5,
			"the fill starts under the first point, not at the edge of the plot");

		vertices.Max().Should().BeApproximately(
			lineVertices.Max(),
			0.5,
			"the fill ends under the last point, not at the edge of the plot");

		// And the padding at each end of a category axis means that is nowhere near the edge.
		vertices.Min().Should().BeGreaterThan(1, "there is a whole category interval before the first point");
	}

	[Theory]
	[InlineData(MarkerStyle.Circle, "circle")]
	[InlineData(MarkerStyle.Square, "rect")]
	[InlineData(MarkerStyle.Diamond, "polygon")]
	[InlineData(MarkerStyle.Triangle, "polygon")]
	[InlineData(MarkerStyle.Cross, "polygon")]
	[InlineData(MarkerStyle.Star4, "polygon")]
	[InlineData(MarkerStyle.Star5, "polygon")]
	[InlineData(MarkerStyle.Star6, "polygon")]
	[InlineData(MarkerStyle.Star10, "polygon")]
	public void EveryMarkerStyle_Renders(MarkerStyle markerStyle, string expectedElement)
	{
		// Issue #30: only Circle was implemented and the rest threw, so a chart asking for
		// square markers failed outright rather than rendering.
		var specification = SingleSeries(
			SeriesChartType.Line,
			Points(10, 24, 17, 31),
			series =>
			{
				series.MarkerStyle = markerStyle;
				series.MarkerSize = 10;
			});

		var document = Render(specification);

		Elements(Defs(document), expectedElement).Should().HaveCount(1, "the marker is defined once and reused");
		Elements(GroupById(document, "series0"), "use").Should().HaveCount(4, "one per point");
	}

	[Fact]
	public void MarkerStyleNone_DefinesNoMarker()
	{
		var document = Render(SingleSeries(SeriesChartType.Line, Points(10, 24, 17, 31)));

		Defs(document).Elements().Should().BeEmpty();
		Elements(GroupById(document, "series0"), "use").Should().BeEmpty();
	}
}
