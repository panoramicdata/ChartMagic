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
}
