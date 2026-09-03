using System.Xml.Linq;
using static PanoramicData.ChartMagic.Test.Support.ChartFixtures;
using static PanoramicData.ChartMagic.Test.Support.RenderedChart;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Tests for issue #33: column and bar rendering, grouped, stacked and to a hundred per cent.
/// </summary>
/// <remarks>
/// These assert on the rendered SVG tree rather than on whether a file appeared. Issue #28 is
/// exactly the failure mode where a chart that draws nothing passes its tests, and every
/// assertion here was first checked against the pre-fix renderer to confirm it fails there.
/// </remarks>
public class ColumnAndBarTests
{
	[Fact]
	public void ColumnChart_DrawsOneRectanglePerPoint()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var series = GroupById(document, "series0");

		var columns = Elements(series, "rect");
		columns.Should().HaveCount(4, "a column chart draws one rectangle per data point");
		columns.Should().AllSatisfy(c => Number(c, "height").Should().BeGreaterThan(0));
		columns.Should().AllSatisfy(c => Number(c, "width").Should().BeGreaterThan(0));
	}

	[Fact]
	public void ColumnChart_ColumnsAreOrderedLeftToRightAndDoNotOverlap()
	{
		var document = Render(ColumnChart(SeriesChartType.Column, 1));
		var columns = Elements(GroupById(document, "series0"), "rect");

		var ordered = columns
			.Select(c => (Left: Number(c, "x"), Right: Number(c, "x") + Number(c, "width")))
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
		var columns = ColumnsByPosition(Render(ColumnChart(SeriesChartType.Column, 1)), "series0");

		Number(columns[3], "height").Should().BeGreaterThan(
			Number(columns[0], "height"),
			"a larger value must produce a taller column");
	}

	[Fact]
	public void GroupedColumns_SitSideBySideWithinTheirCategory()
	{
		var firstOfEachSeries = FirstColumnOfEachSeries(Render(ColumnChart(SeriesChartType.Column, 3)), 3);

		firstOfEachSeries.Select(c => Number(c, "x")).Should().BeInAscendingOrder(
			"each grouped series takes the next slot within the band");
		firstOfEachSeries.Select(c => Number(c, "x")).Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void StackedColumns_ShareASlotAndStackVertically()
	{
		var firstOfEachSeries = FirstColumnOfEachSeries(
			Render(ColumnChart(SeriesChartType.StackedColumn, 3)),
			3);

		firstOfEachSeries.Select(c => Number(c, "x")).Distinct().Should().HaveCount(
			1,
			"stacked series occupy the same slot rather than standing side by side");

		// Each segment starts above the one below it: SVG y increases downwards, so a segment
		// stacked on top has the smaller y.
		firstOfEachSeries.Select(c => Number(c, "y")).Should().BeInDescendingOrder();
	}

	[Fact]
	public void ColumnChart_ValueAxisStartsAtZero()
	{
		// The smallest value in the sample data is 10 and the largest 31. An axis running from
		// 10 to 31 would make the 24 column look three times the 10 column.
		var document = Render(ColumnChart(SeriesChartType.Column, 1));

		NumericLabels(document, "yAxis")
			.Should()
			.Contain(0, "a column is read by its length, so its axis must start at zero");

		// Heights are then proportional to values: 31 against 10, within rounding.
		var columns = ColumnsByPosition(document, "series0");

		(Number(columns[3], "height") / Number(columns[0], "height"))
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

		AllColumns(document, 3).Should().AllSatisfy(
			segment => (Number(segment, "y") + Number(segment, "height"))
				.Should()
				.BeLessThanOrEqualTo(plotHeight + 0.5, "no segment may extend past the plot floor"));
	}

	[Fact]
	public void BarChart_DrawsHorizontalRectangles()
	{
		var bars = Elements(GroupById(Render(ColumnChart(SeriesChartType.Bar, 1)), "series0"), "rect")
			.OrderBy(b => Number(b, "y"))
			.ToList();

		bars.Should().HaveCount(4);

		// Bars share a left edge and vary in width; columns would share a bottom edge and vary
		// in height.
		bars.Select(b => Number(b, "x")).Distinct().Should().HaveCount(1);
		bars.Select(b => Number(b, "width")).Distinct().Should().HaveCountGreaterThan(1);
		bars.Select(b => Number(b, "y")).Should().BeInAscendingOrder();
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

		var bars = Elements(GroupById(Render(specification), "series0"), "rect")
			.Where(r => Number(r, "width") > 0)
			.OrderBy(r => Number(r, "y"))
			.ToList();

		bars.Should().HaveCount(Categories.Length);

		// Top to bottom, so the longest bar - the first category - comes last.
		bars.Select(r => Number(r, "width")).Should().BeInAscendingOrder(
			"the first category, with the longest bar, belongs at the bottom");
	}

	/// <summary>
	/// A hundred per cent stacked column fills the plot, with every category summing to the top.
	/// </summary>
	/// <remarks>
	/// These drew nothing at all: the chart types were in the enumeration but in none of the sets
	/// that decide what is banded and what stacks, so every series fell through. The reference
	/// renderer draws them, so a blank chart is the most conspicuous difference there is.
	///
	/// The values are shares of their category rather than amounts, so what is asserted is that
	/// each category is full: the top of the topmost segment reaches the same height everywhere,
	/// whatever the underlying totals.
	/// </remarks>
	[Fact]
	public void PercentStackedColumns_FillEveryCategoryToTheSameHeight()
	{
		var document = Render(ColumnChart(SeriesChartType.StackedColumn100, 3));

		var columns = AllColumns(document, 3)
			.Where(rect => Number(rect, "height") > 0)
			.ToList();

		columns.Should().NotBeEmpty("a hundred per cent stacked column chart draws columns");

		// Grouped by category, using the left edge, since the three series share a slot.
		var byCategory = columns
			.GroupBy(rect => Math.Round(Number(rect, "x")))
			.ToList();

		byCategory.Should().HaveCount(Categories.Length);

		// The top of the tallest segment in each category, which is where the stack finishes.
		var tops = byCategory
			.Select(group => group.Min(rect => Number(rect, "y")))
			.ToList();

		tops.Max().Should().BeApproximately(
			tops.Min(),
			1.5,
			"every category is full, so every stack finishes at the same height");

		// And full means the top of the axis, not some height derived from the data.
		tops.Min().Should().BeLessThan(
			Height * 0.1,
			"a full stack reaches the top of the plot");
	}

	/// <summary>
	/// The rectangles of one series, in the order they are drawn across the plot.
	/// </summary>
	private static List<XElement> ColumnsByPosition(XDocument document, string seriesId)
		=> [.. Elements(GroupById(document, seriesId), "rect").OrderBy(c => Number(c, "x"))];

	/// <summary>
	/// The leftmost rectangle of each of the first several series, which is the first category.
	/// </summary>
	private static List<XElement> FirstColumnOfEachSeries(XDocument document, int seriesCount)
		=> [.. Enumerable
			.Range(0, seriesCount)
			.Select(index => ColumnsByPosition(document, $"series{index}")[0])];

	/// <summary>
	/// Every rectangle drawn by the first several series.
	/// </summary>
	private static List<XElement> AllColumns(XDocument document, int seriesCount)
		=> [.. Enumerable
			.Range(0, seriesCount)
			.SelectMany(index => Elements(GroupById(document, $"series{index}"), "rect"))];
}
