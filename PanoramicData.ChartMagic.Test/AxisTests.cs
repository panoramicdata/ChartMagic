using PanoramicData.ChartMagic.Renderers;
using System.Globalization;
using System.Xml.Linq;
using static PanoramicData.ChartMagic.Test.Support.ChartFixtures;
using static PanoramicData.ChartMagic.Test.Support.RenderedChart;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Tests for issue #31: the axis furniture - lines, ticks, labels, titles, gridlines and the
/// choice of scale.
/// </summary>
/// <remarks>
/// These assert on the rendered SVG tree rather than on whether a file appeared. Issue #28 is
/// exactly the failure mode where a chart that draws nothing passes its tests, and every
/// assertion here was first checked against the pre-fix renderer to confirm it fails there.
/// </remarks>
public class AxisTests
{
	[Fact]
	public void LineChartWithCategoryLabels_KeepsCategoriesOnTheXAxis()
	{
		// A line chart has no banded series, and TrueForAll is vacuously true on an empty
		// list - which briefly made every labelled line chart render as a bar chart, with its
		// categories down the Y axis and its values along the X.
		var document = Render(SingleSeries(SeriesChartType.Line, Points(10, 24, 17, 31)));

		LabelTexts(document, "xAxis")
			.Should()
			.BeEquivalentTo(Categories, "the categories belong on the X axis for a line chart");

		LabelTexts(document, "yAxis")
			.Should()
			.AllSatisfy(label => double.TryParse(label, CultureInfo.InvariantCulture, out _).Should().BeTrue(
				"the Y axis carries the values"));
	}

	[Fact]
	public void LineChartWithCategoryLabels_LabelsEveryCategory()
	{
		// Generated numeric intervals over seven categories showed every other one.
		var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		var specification = SingleSeries(
			SeriesChartType.Line,
			[.. days.Select((day, index) => new ChartPoint(day, index, 10 + index))]);

		LabelTexts(Render(specification), "xAxis").Should().BeEquivalentTo(days);
	}

	[Fact]
	public void XAxis_DrawsALineTicksAndOneLabelPerCategory()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));

		Elements(GroupById(document, "xAxis"), "line").Should().HaveCount(
			5,
			"the axis line itself, plus one tick per category");

		LabelTexts(document, "xAxis")
			.Should()
			.BeEquivalentTo(Categories, "the category labels come from the data");
	}

	[Fact]
	public void YAxis_LabelsAreOrderedFromTheTopDown()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var labels = Elements(GroupById(document, "yAxis"), "text")
			.OrderBy(t => Number(t, "y"))
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

		LabelTexts(document, "xAxis").Should().Contain("Month");

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
	public void FontSize_IsEmitted()
	{
		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.XAxisFontSize = 11;

		var labels = Elements(GroupById(Render(specification), "xAxis"), "text");

		labels.Should().HaveCount(4, "an empty collection would satisfy the assertion below");
		labels.Should().AllSatisfy(
			t => t.Attribute("font-size")!.Value.Should().Be("11", "the font size was carried and never used"));
	}

	[Fact]
	public void MajorGridlines_AreDrawnAcrossThePlotOnlyWhenAskedFor()
	{
		FindGroupById(Render(ColumnChart(SeriesChartType.Column, 1)), "gridlines")
			.Should()
			.BeNull("gridlines are opt-in");

		var specification = ColumnChart(SeriesChartType.Column, 1);
		specification.YAxisMajorGridEnabled = true;

		var gridlines = Elements(GroupById(Render(specification), "gridlines"), "line");
		gridlines.Should().HaveCountGreaterThan(1);
		gridlines.Should().AllSatisfy(
			l => Number(l, "y1").Should().Be(Number(l, "y2")),
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

		var labels = LabelTexts(Render(specification), "yAxis");

		labels.Should().NotBeEmpty();
		labels.Should().AllSatisfy(l => l.Should().Contain(".", "every label carries one decimal place"));
	}

	[Fact]
	public void ShortLabels_AbbreviateThousands()
	{
		var specification = SingleSeries(
			SeriesChartType.Column,
			Points(12_000, 24_000, 17_000, 31_000));
		specification.UseYAxisShortLabels = true;

		LabelTexts(Render(specification), "yAxis").Should().Contain(l => l.EndsWith('K'));
	}

	[Fact]
	public void LogarithmicYAxis_LabelsWholeDecades()
	{
		var specification = LogarithmicSpecification(logarithmic: true);

		NumericLabels(Render(specification), "yAxis")
			.OrderBy(v => v)
			.Should()
			.Equal([1, 10, 100, 1000], "issue #31: a logarithmic axis is labelled in decades");
	}

	[Fact]
	public void LogarithmicYAxis_SpreadsSmallValuesAwayFromTheFloor()
	{
		// On a linear axis the value 2 out of 900 is pinned to the bottom of the plot; on a
		// logarithmic one it should sit a third of the way up. This is the visible symptom the
		// demo shows for issue #31.
		var linearY = FirstPointY(Render(LogarithmicSpecification(logarithmic: false)));
		var logarithmicY = FirstPointY(Render(LogarithmicSpecification(logarithmic: true)));

		logarithmicY.Should().BeLessThan(
			linearY - 20,
			"the flag must change the mapping, not merely be accepted");
	}

	/// <summary>
	/// The value axis chooses the step and bounds the reference renderer chooses.
	/// </summary>
	/// <remarks>
	/// Every row was read off a reference render of that data, and together they are what the rule
	/// is derived from rather than an illustration of it. The last row is the case the previous
	/// rule already got right, kept so that fixing the negative cases cannot quietly break the
	/// positive one - which is how the previous rule came to be wrong in the first place: it was
	/// fitted to positive data alone and read the step off the larger extreme, which is the same
	/// number as the span only while the data stays on one side of zero.
	/// </remarks>
	[Theory]
	[InlineData(-11, 26, 10, -20, 30)]
	[InlineData(-30, 12, 10, -40, 20)]
	[InlineData(-2, 9, 2, -4, 10)]
	[InlineData(0, 30, 5, 0, 35)]
	public void ValueAxis_UsesTheMeasuredStepAndBounds(
		double dataMinimum,
		double dataMaximum,
		double expectedStep,
		double expectedStart,
		double expectedEnd)
	{
		var (step, start, end) = TickGenerator.LinearBounds(dataMinimum, dataMaximum);

		step.Should().Be(expectedStep, "the step measured for data from {0} to {1}", dataMinimum, dataMaximum);
		start.Should().Be(expectedStart);
		end.Should().Be(expectedEnd);
	}

	/// <summary>
	/// Moving the plot moves its axes with it.
	/// </summary>
	/// <remarks>
	/// An axis frame is not an independent rectangle - it has to line up with the plot along the
	/// dimension they share, or its ticks and labels point at the wrong values. The axis areas
	/// carried their own defaults, 10% in and 90% long, which did not track the plot: measured on a
	/// chart with the report defaults, the value axis line ran from y 59 to 360 where the reference
	/// render drew it from 40 to 339 - exactly the 5% of the height by which the plot had moved.
	///
	/// Built as a Chart rather than from a ChartSpecification on purpose. ToChart already assigns
	/// the axis positions from the inner plot, so it hides this: a first version of this test
	/// passed with the fix stashed. Callers that build a Chart directly - which is how the
	/// specification translator in Magic Suite drives this library - had no such protection.
	/// </remarks>
	[Fact]
	public void ValueAxis_SharesTheVerticalExtentOfThePlot()
	{
		var chart = new Chart();

		// A plot deliberately not at the axis areas' default 10% and 90%.
		chart.ChartArea.InnerPlot.YPositionPercent = 25;
		chart.ChartArea.InnerPlot.HeightPercent = 60;

		chart.Series.Add(new Series(chart.ChartArea.InnerPlot, "S")
		{
			ChartType = SeriesChartType.Column,
			FillColor = System.Drawing.Color.SteelBlue,
			Points = Points(10, 24, 17, 31)
		});

		var document = Render(chart);

		TranslationY(GroupById(document, "yAxis")).Should().BeApproximately(
			TranslationY(GroupById(document, "innerPlot")),
			0.5,
			"the value axis starts where the plot it annotates starts");
	}

	/// <summary>
	/// The same data, plotted with the logarithmic flag on or off.
	/// </summary>
	private static ChartSpecification LogarithmicSpecification(bool logarithmic)
	{
		var specification = SingleSeries(
			SeriesChartType.Line,
			[new(null, 0, 2), new(null, 1, 40), new(null, 2, 900), new(null, 3, 30)],
			series => series.StrokeColor = System.Drawing.Color.Crimson);
		specification.YAxisIsLogarithmic = logarithmic;
		return specification;
	}

	/// <summary>
	/// Where the first point of the first series was drawn vertically.
	/// </summary>
	private static double FirstPointY(XDocument document)
	{
		var path = Elements(GroupById(document, "series0"), "path")[0].Attribute("d")!.Value;
		return double.Parse(path.Split(' ')[1], CultureInfo.InvariantCulture);
	}
}
