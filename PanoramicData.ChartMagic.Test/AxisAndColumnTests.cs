using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Tests for issues #31 (axis furniture) and #33 (column and bar rendering), and for the legend
/// layout in #35.
/// </summary>
/// <remarks>
/// These assert on the rendered SVG tree rather than on whether a file appeared. Issue #28 is
/// exactly the failure mode where a chart that draws nothing passes its tests, and every
/// assertion here was first checked against the pre-fix renderer to confirm it fails there.
/// </remarks>
public class AxisAndColumnTests
{
	private const int Width = 800;
	private const int Height = 400;

	private static readonly string[] Categories = ["Jan", "Feb", "Mar", "Apr"];

	private static XDocument Render(ChartSpecification specification)
	{
		using var stream = new MemoryStream();
		specification.ToChart().SaveImage(stream, ChartImageFormat.Svg, Width, Height);
		return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
	}

	private static XElement GroupById(XDocument document, string id)
		=> document
			.Descendants()
			.First(e => e.Name.LocalName == "g" && e.Attribute("id")?.Value == id);

	private static List<XElement> Elements(XElement parent, string localName)
		=> parent.Descendants().Where(e => e.Name.LocalName == localName).ToList();

	/// <summary>
	/// The X co-ordinates of the vertices of a path, in order.
	/// </summary>
	/// <remarks>
	/// A line series is one path of move-and-line commands rather than an element per point, so
	/// the point positions have to be read out of the geometry.
	/// </remarks>
	private static List<double> PathVertexXValues(XElement path)
		=> [.. path
			.Attribute("d")!.Value
			.Split(['M', 'L'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(segment => segment.Split([',', ' '])[0])
			.Select(x => double.Parse(x, CultureInfo.InvariantCulture))];
	private static double Attribute(XElement element, string name)
		=> double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

	private static List<ChartPoint> Points(params double[] values)
		=> [.. values.Select((value, index) => new ChartPoint(Categories[index], index, value))];

	private static ChartSpecification ColumnChart(SeriesChartType chartType, int seriesCount)
	{
		var palette = new[] { Color.SteelBlue, Color.SeaGreen, Color.Goldenrod };
		return new ChartSpecification
		{
			SeriesList =
			[
				.. Enumerable.Range(0, seriesCount).Select(i => new SeriesSpecification
				{
					ChartType = chartType,
					FillColor = palette[i % palette.Length],
					StrokeColor = palette[i % palette.Length],
					Points = Points(10 + (i * 4), 24 - (i * 3), 17 + i, 31 - (i * 5)),
				})
			]
		};
	}

	[Fact]
	public void ColumnChart_DrawsOneRectanglePerPoint()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var series = GroupById(document, "series0");

		var columns = Elements(series, "rect");
		columns.Should().HaveCount(4, "a column chart draws one rectangle per data point");
		columns.Should().AllSatisfy(c => Attribute(c, "height").Should().BeGreaterThan(0));
		columns.Should().AllSatisfy(c => Attribute(c, "width").Should().BeGreaterThan(0));
	}

	[Fact]
	public void ColumnChart_ColumnsAreOrderedLeftToRightAndDoNotOverlap()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var columns = Elements(GroupById(document, "series0"), "rect");

		var ordered = columns
			.Select(c => (Left: Attribute(c, "x"), Right: Attribute(c, "x") + Attribute(c, "width")))
			.OrderBy(c => c.Left)
			.ToList();

		// Without a count assertion this passes on an empty render: an empty sequence is both
		// ordered and non-overlapping. That is the issue #28 trap.
		ordered.Should().HaveCount(4);
		ordered.Select(c => c.Left).Should().BeInAscendingOrder();
		for (var i = 1; i < ordered.Count; i++)
		{
			ordered[i].Left.Should().BeGreaterThanOrEqualTo(
				ordered[i - 1].Right,
				"columns in different categories must not overlap");
		}
	}

	[Fact]
	public void ColumnChart_TallerValueDrawsATallerColumn()
	{
		// The fourth value (31) is the largest and the first (10) the smallest.
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var columns = Elements(GroupById(document, "series0"), "rect")
			.OrderBy(c => Attribute(c, "x"))
			.ToList();

		Attribute(columns[3], "height").Should().BeGreaterThan(
			Attribute(columns[0], "height"),
			"a larger value must produce a taller column");
	}

	[Fact]
	public void GroupedColumns_SitSideBySideWithinTheirCategory()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 3));

		var firstOfEachSeries = Enumerable.Range(0, 3)
			.Select(i => Elements(GroupById(document, $"series{i}"), "rect").OrderBy(c => Attribute(c, "x")).First())
			.ToList();

		firstOfEachSeries.Select(c => Attribute(c, "x")).Should().BeInAscendingOrder(
			"each grouped series takes the next slot within the band");
		firstOfEachSeries.Select(c => Attribute(c, "x")).Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void StackedColumns_ShareASlotAndStackVertically()
	{
		var document = Render(ColumnChart(SeriesChartType.StackedColumn, 3));

		var firstOfEachSeries = Enumerable.Range(0, 3)
			.Select(i => Elements(GroupById(document, $"series{i}"), "rect").OrderBy(c => Attribute(c, "x")).First())
			.ToList();

		firstOfEachSeries.Select(c => Attribute(c, "x")).Distinct().Should().HaveCount(
			1,
			"stacked series occupy the same slot rather than standing side by side");

		// Each segment starts above the one below it: SVG y increases downwards, so a segment
		// stacked on top has the smaller y.
		firstOfEachSeries.Select(c => Attribute(c, "y")).Should().BeInDescendingOrder();
	}

	[Fact]
	public void ColumnChart_ValueAxisStartsAtZero()
	{
		// The smallest value in the sample data is 10 and the largest 31. An axis running from
		// 10 to 31 would make the 24 column look three times the 10 column.
		var document = Render(ColumnChart(SeriesChartType.Column, 1));

		var labels = Elements(GroupById(document, "yAxis"), "text")
			.Select(t => double.Parse(t.Value, CultureInfo.InvariantCulture))
			.ToList();

		labels.Should().Contain(0, "a column is read by its length, so its axis must start at zero");

		// Heights are then proportional to values: 31 against 10, within rounding.
		var columns = Elements(GroupById(document, "series0"), "rect")
			.OrderBy(c => Attribute(c, "x"))
			.ToList();

		(Attribute(columns[3], "height") / Attribute(columns[0], "height"))
			.Should()
			.BeApproximately(31d / 10d, 0.05);
	}

	[Fact]
	public void StackedColumns_StayInsideThePlot()
	{
		// With the origin off the plot, the first segment of each stack was drawn below the
		// floor and over the category labels.
		var document = Render(ColumnChart(SeriesChartType.StackedColumn, 3));
		// The chart area is full height and the inner plot is 90% of it.
		var plotHeight = Height * 0.9;

		for (var i = 0; i < 3; i++)
		{
			foreach (var segment in Elements(GroupById(document, $"series{i}"), "rect"))
			{
				(Attribute(segment, "y") + Attribute(segment, "height"))
					.Should()
					.BeLessThanOrEqualTo(plotHeight + 0.5, "no segment may extend past the plot floor");
			}
		}
	}

	[Fact]
	public void BarChart_DrawsHorizontalRectangles()
	{
		var document = Render(ColumnChart(SeriesChartType.Bar, 1));
		var bars = Elements(GroupById(document, "series0"), "rect")
			.OrderBy(b => Attribute(b, "y"))
			.ToList();

		bars.Should().HaveCount(4);

		// Bars share a left edge and vary in width; columns would share a bottom edge and vary
		// in height.
		bars.Select(b => Attribute(b, "x")).Distinct().Should().HaveCount(1);
		bars.Select(b => Attribute(b, "width")).Distinct().Should().HaveCountGreaterThan(1);
		bars.Select(b => Attribute(b, "y")).Should().BeInAscendingOrder();
	}

	[Fact]
	public void LineChartWithCategoryLabels_KeepsCategoriesOnTheXAxis()
	{
		// A line chart has no banded series, and TrueForAll is vacuously true on an empty
		// list - which briefly made every labelled line chart render as a bar chart, with its
		// categories down the Y axis and its values along the X.
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					StrokeColor = Color.SteelBlue,
					Points = Points(10, 24, 17, 31),
				}
			]
		};

		var document = Render(specification);

		Elements(GroupById(document, "xAxis"), "text")
			.Select(t => t.Value)
			.Should()
			.BeEquivalentTo(Categories, "the categories belong on the X axis for a line chart");

		Elements(GroupById(document, "yAxis"), "text")
			.Should()
			.AllSatisfy(t => double.TryParse(t.Value, CultureInfo.InvariantCulture, out _).Should().BeTrue(
				"the Y axis carries the values"));
	}

	[Fact]
	public void LineChartWithCategoryLabels_LabelsEveryCategory()
	{
		// Generated numeric intervals over seven categories showed every other one.
		var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					StrokeColor = Color.SteelBlue,
					Points = [.. days.Select((d, i) => new ChartPoint(d, i, 10 + i))],
				}
			]
		};

		Elements(GroupById(Render(specification), "xAxis"), "text")
			.Select(t => t.Value)
			.Should()
			.BeEquivalentTo(days);
	}

	[Fact]
	public void XAxis_DrawsALineTicksAndOneLabelPerCategory()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var xAxis = GroupById(document, "xAxis");

		Elements(xAxis, "line").Should().HaveCount(
			5,
			"the axis line itself, plus one tick per category");

		Elements(xAxis, "text")
			.Select(t => t.Value)
			.Should()
			.BeEquivalentTo(Categories, "the category labels come from the data");
	}

	[Fact]
	public void YAxis_LabelsAreOrderedFromTheTopDown()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var labels = Elements(GroupById(document, "yAxis"), "text")
			.OrderBy(t => Attribute(t, "y"))
			.Select(t => double.Parse(t.Value, CultureInfo.InvariantCulture))
			.ToList();

		labels.Should().HaveCountGreaterThan(1);
		labels.Should().BeInDescendingOrder("the largest value sits at the top of the axis");
	}

	[Fact]
	public void AxisTitles_AreRendered()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.XAxisTitle = "Month";
		specification.YAxisTitle = "Widgets";

		var document = Render(specification);

		Elements(GroupById(document, "xAxis"), "text").Select(t => t.Value).Should().Contain("Month");

		var yTitle = Elements(GroupById(document, "yAxis"), "text").FirstOrDefault(t => t.Value == "Widgets");
		yTitle.Should().NotBeNull("issue #31: the Y axis title was accepted and never drawn");
		yTitle!.Attribute("transform")!.Value.Should().Contain("rotate(-90", "a Y axis title reads bottom-to-top");
	}

	[Fact]
	public void LabelAngle_RotatesTheTickLabels()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.XAxisLabelAngle = -45;

		var labels = Elements(GroupById(Render(specification), "xAxis"), "text");

		// AllSatisfy is vacuously true on an empty collection, so the count comes first.
		labels.Should().HaveCount(4);
		labels.Should().AllSatisfy(l => l.Attribute("transform")!.Value.Should().Contain("rotate(-45"));
	}

	[Fact]
	public void MajorGridlines_AreDrawnAcrossThePlotOnlyWhenAskedFor()
	{
		var withoutGrid = Render(ColumnChart(SeriesChartType.Column, 1));
		withoutGrid.Descendants()
			.Any(e => e.Attribute("id")?.Value == "gridlines")
			.Should()
			.BeFalse("gridlines are opt-in");

		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.YAxisMajorGridEnabled = true;

		var gridlines = Elements(GroupById(Render(specification), "gridlines"), "line");
		gridlines.Should().HaveCountGreaterThan(1);
		gridlines.Should().AllSatisfy(
			l => Attribute(l, "y1").Should().Be(Attribute(l, "y2")),
			"Y axis gridlines run horizontally");
	}

	[Fact]
	public void ShortLabels_UseOneDecimalPlace()
	{
		// Measured against DocMagic: with short labels on and values topping out at 35, its axis
		// reads 35.0, 30.0, 25.0. Abbreviating only above a thousand, as this did, left a
		// percentage axis untouched and made the setting look ignored.
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.UseYAxisShortLabels = true;

		var labels = Elements(GroupById(Render(specification), "yAxis"), "text")
			.Select(t => t.Value)
			.ToList();

		labels.Should().NotBeEmpty();
		labels.Should().AllSatisfy(l => l.Should().Contain(".", "every label carries one decimal place"));
	}

	[Fact]
	public void ShortLabels_AbbreviateThousands()
	{
		var specification = new ChartSpecification
		{
			UseYAxisShortLabels = true,
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					FillColor = Color.SteelBlue,
					StrokeColor = Color.SteelBlue,
					Points = Points(12_000, 24_000, 17_000, 31_000),
				}
			]
		};

		Elements(GroupById(Render(specification), "yAxis"), "text")
			.Select(t => t.Value)
			.Should()
			.Contain(l => l.EndsWith('K'));
	}

	[Fact]
	public void LogarithmicYAxis_LabelsWholeDecades()
	{
		var specification = new ChartSpecification
		{
			YAxisIsLogarithmic = true,
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					StrokeColor = Color.Crimson,
					Points = [new(null, 0, 2), new(null, 1, 40), new(null, 2, 900), new(null, 3, 30)],
				}
			]
		};

		var labels = Elements(GroupById(Render(specification), "yAxis"), "text")
			.Select(t => double.Parse(t.Value, CultureInfo.InvariantCulture))
			.OrderBy(v => v)
			.ToList();

		labels.Should().Equal([1, 10, 100, 1000], "issue #31: a logarithmic axis is labelled in decades");
	}

	[Fact]
	public void LogarithmicYAxis_SpreadsSmallValuesAwayFromTheFloor()
	{
		var points = new List<ChartPoint> { new(null, 0, 2), new(null, 1, 40), new(null, 2, 900) };

		ChartSpecification Build(bool logarithmic) => new()
		{
			YAxisIsLogarithmic = logarithmic,
			SeriesList = [new() { ChartType = SeriesChartType.Line, StrokeColor = Color.Crimson, Points = points }]
		};

		static double FirstPointY(XDocument document)
		{
			var path = Elements(GroupById(document, "series0"), "path")[0].Attribute("d")!.Value;
			var firstPoint = path.Split(' ')[1];
			return double.Parse(firstPoint, CultureInfo.InvariantCulture);
		}

		// On a linear axis the value 2 out of 900 is pinned to the bottom of the plot; on a
		// logarithmic one it should sit a third of the way up. This is the visible symptom the
		// demo shows for issue #31.
		var linearY = FirstPointY(Render(Build(logarithmic: false)));
		var logarithmicY = FirstPointY(Render(Build(logarithmic: true)));

		logarithmicY.Should().BeLessThan(
			linearY - 20,
			"the flag must change the mapping, not merely be accepted");
	}

	[Fact]
	public void LegendLabels_DoNotOverlap()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 3));
		var legend = GroupById(document, "legend");

		var labels = Elements(legend, "text").OrderBy(t => Attribute(t, "x")).ToList();
		labels.Should().HaveCount(3);

		// Issue #35: the labels used to be spaced by a fraction of their intended distance and
		// sat on top of one another. Each label needs at least its own width of room, and at
		// the default font size "Series 1" is about eight characters wide.
		var minimumSpacing = 8 * 20 * 0.5;
		for (var i = 1; i < labels.Count; i++)
		{
			(Attribute(labels[i], "x") - Attribute(labels[i - 1], "x"))
				.Should()
				.BeGreaterThan(minimumSpacing, "adjacent legend labels must not overlap");
		}
	}

	[Fact]
	public void LegendLabels_AreNotOutlinedInBlack()
	{
		var legend = GroupById(Render(ColumnChart(SeriesChartType.Column, 2)), "legend");

		Elements(legend, "text").Should().AllSatisfy(
			t => t.Attribute("stroke").Should().BeNull(
				"issue #35: a transparent stroke colour became a black outline on every label"));
	}

	[Fact]
	public void LegendLabels_UseTheLegendTextWhenGiven()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.SeriesList[0].LegendText = "Widgets sold";

		Elements(GroupById(Render(specification), "legend"), "text")
			.Select(t => t.Value)
			.Should()
			.Contain("Widgets sold");
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
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					StrokeColor = Color.SteelBlue,
					MarkerStyle = markerStyle,
					MarkerSize = 10,
					Points = Points(10, 24, 17, 31),
				}
			]
		};

		var document = Render(specification);
		var defs = document.Descendants().First(e => e.Name.LocalName == "defs");

		Elements(defs, expectedElement).Should().HaveCount(1, "the marker is defined once and reused");
		Elements(GroupById(document, "series0"), "use").Should().HaveCount(4, "one per point");
	}

	[Fact]
	public void MarkerStyleNone_DefinesNoMarker()
	{
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new() { ChartType = SeriesChartType.Line, StrokeColor = Color.SteelBlue, Points = Points(10, 24, 17, 31) }
			]
		};

		var document = Render(specification);

		document.Descendants().First(e => e.Name.LocalName == "defs").Elements().Should().BeEmpty();
		Elements(GroupById(document, "series0"), "use").Should().BeEmpty();
	}

	[Fact]
	public void FontSize_IsEmitted()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.XAxisFontSize = 11;

		var labels = Elements(GroupById(Render(specification), "xAxis"), "text");

		labels.Should().HaveCount(4, "an empty collection would satisfy the assertion below");
		labels.Should().AllSatisfy(
			t => t.Attribute("font-size")!.Value.Should().Be("11", "the font size was carried and never used"));
	}

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
		var document = Render(ColumnChart(chartType, 1));
		var series = GroupById(document, "series0");

		// A column is positioned by its left edge and centred on its category, so the centre has
		// to be reconstructed; a line passes through the centres already, so they are read off
		// the vertices of its path.
		var centres = chartType == SeriesChartType.Column
			? [.. Elements(series, "rect")
				.Select(r => Attribute(r, "x") + (Attribute(r, "width") / 2))
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

	/// <summary>
	/// The X component of a group's translation, or zero where it has none.
	/// </summary>
	private static double TranslationX(XElement group)
	{
		var transform = group.Attribute("transform")?.Value;
		if (transform is null)
		{
			return 0;
		}

		var inside = transform[(transform.IndexOf('(') + 1)..transform.IndexOf(')')];
		return double.Parse(inside.Split(',')[0], CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// A bar chart puts its first category at the bottom.
	/// </summary>
	/// <remarks>
	/// The category axis of a horizontal plot runs upwards, which is the opposite of the order the
	/// categories are given in and of how a column chart lays them out left to right. Measured on a
	/// seven-day bar chart: reading the bar lengths off the reference render from the top gave
	/// Sunday first and Monday last. Drawing them in the order given put every bar against the
	/// wrong label - the chart was not subtly out, it was reporting the wrong days.
	/// </remarks>
	[Fact]
	public void BarChart_PutsTheFirstCategoryAtTheBottom()
	{
		// Descending values, so the first category is identifiable by having the longest bar
		// whichever end it is drawn at.
		var specification = ColumnChart(SeriesChartType.Bar, 1);
		specification.SeriesList[0].Points = Points(40, 30, 20, 10);

		var document = Render(specification);

		var bars = Elements(GroupById(document, "series0"), "rect")
			.Where(r => Attribute(r, "width") > 0)
			.OrderBy(r => Attribute(r, "y"))
			.ToList();

		bars.Should().HaveCount(Categories.Length);

		// Top to bottom, so the longest bar - the first category - comes last.
		bars.Select(r => Attribute(r, "width")).Should().BeInAscendingOrder(
			"the first category, with the longest bar, belongs at the bottom");
	}
}