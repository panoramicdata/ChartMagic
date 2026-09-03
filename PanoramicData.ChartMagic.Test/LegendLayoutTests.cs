using System.Xml.Linq;
using static PanoramicData.ChartMagic.Test.Support.ChartFixtures;
using static PanoramicData.ChartMagic.Test.Support.RenderedChart;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Tests for the legend layout in issue #35.
/// </summary>
/// <remarks>
/// The numbers here were measured off reference renders rather than chosen, and the remarks on
/// each test say which render and what the previous layout produced instead.
/// </remarks>
public class LegendLayoutTests
{
	[Fact]
	public void LegendLabels_DoNotOverlap()
	{
		var legend = GroupById(Render(ColumnChart(SeriesChartType.Column, 3)), "legend");

		var labels = Elements(legend, "text").OrderBy(t => Number(t, "x")).ToList();
		labels.Should().HaveCount(3);

		// Issue #35: the labels used to be spaced by a fraction of their intended distance and
		// sat on top of one another. Each label needs at least its own width of room, and at
		// the default font size "Series 1" is about eight characters wide.
		var minimumSpacing = 8 * 20 * 0.5;
		for (var i = 1; i < labels.Count; i++)
		{
			(Number(labels[i], "x") - Number(labels[i - 1], "x"))
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

		LabelTexts(Render(specification), "legend").Should().Contain("Widgets sold");
	}

	/// <summary>
	/// A column legend matches the reference render: rectangular swatches sized from the font,
	/// spread down the legend, inset from its left edge.
	/// </summary>
	/// <remarks>
	/// Every number here was measured off a reference render of the same chart - 720 by 400, a
	/// legend occupying the right 20% at full height, three series at font size 12 - rather than
	/// chosen. That render put 32 by 14 swatches at x 594, with row centres at y 69.5, 198.5 and
	/// 327.5.
	///
	/// This drew 9 by 9 swatches at x 622 with centres 20 apart around the middle: less than a
	/// third of the swatch area, and three entries huddled in the centre of an otherwise empty
	/// legend. It is the residual difference on every case in the corpus that draws a legend, which
	/// is most of them.
	///
	/// Tolerances are two pixels: the reference is a rasterised render measured by colour
	/// threshold, so its own edges are only good to about a pixel.
	/// </remarks>
	[Fact]
	public void ColumnLegend_MatchesTheMeasuredReferenceLayout()
	{
		const double ImageWidth = 720;
		const double ImageHeight = 400;
		const double FontSize = 12;

		var specification = ColumnChart(SeriesChartType.Column, 3);
		specification.LegendStyle = LegendStyle.Column;
		specification.LegendXPositionPercent = 80;
		specification.LegendYPositionPercent = 0;
		specification.LegendWidthPercent = 20;
		specification.LegendHeightPercent = 100;
		specification.LegendFontSize = FontSize;
		specification.ChartAreaWidthPercent = 80;

		var document = Render(specification, (int)ImageWidth, (int)ImageHeight);
		var entries = SwatchesOf(document, ImageWidth * 0.2)
			.OrderBy(r => Number(r, "y"))
			.ToList();
		entries.Should().HaveCount(3);

		foreach (var entry in entries)
		{
			Number(entry, "width").Should().BeApproximately(32, 2, "the reference swatch is 32 wide");
			Number(entry, "height").Should().BeApproximately(14, 2, "the reference swatch is 14 tall");
			Number(entry, "x").Should().BeApproximately(
				ImageWidth * 0.2 * 0.12,
				2,
				"the reference inset the swatch 18 pixels into a 144-wide legend");
		}

		// Centres, which is where the spread shows.
		var centres = entries
			.Select(r => Number(r, "y") + (Number(r, "height") / 2))
			.ToList();

		centres[0].Should().BeApproximately(69.5, 2);
		centres[1].Should().BeApproximately(198.5, 2);
		centres[2].Should().BeApproximately(327.5, 2);
	}

	/// <summary>
	/// A row legend packs its entries and centres them, leaving room for each label.
	/// </summary>
	/// <remarks>
	/// Two things at once, because they were broken by the same line. The reference render packs
	/// the entries and centres the result rather than giving each an equal share of the width; and
	/// a slot sized without reference to what goes in it put the next swatch on top of the
	/// previous label once the swatch became a rectangle rather than a small square. It was plainly
	/// visible in the demo: "Memor" then a coloured block over the rest of the word.
	///
	/// The room-for-the-label assertion deliberately uses a smaller per-character estimate than
	/// the layout does, so it checks that room was left rather than restating how much.
	/// </remarks>
	[Fact]
	public void RowLegend_PacksEntriesWithoutOverlappingTheirLabels()
	{
		const double FontSize = 12;
		string[] labels = ["CPU", "Memory", "Disk"];

		var specification = ColumnChart(SeriesChartType.Column, 3);
		for (var index = 0; index < labels.Length; index++)
		{
			specification.SeriesList[index].LegendText = labels[index];
		}

		specification.LegendStyle = LegendStyle.Row;
		specification.LegendXPositionPercent = 0;
		specification.LegendYPositionPercent = 0;
		specification.LegendWidthPercent = 100;
		specification.LegendHeightPercent = 15;
		specification.LegendFontSize = FontSize;

		var swatches = SwatchesOf(Render(specification), Width / 2.0)
			.OrderBy(r => Number(r, "x"))
			.ToList();

		swatches.Should().HaveCount(labels.Length);

		// Each entry leaves room for its own label before the next one starts.
		for (var index = 0; index < swatches.Count - 1; index++)
		{
			var swatchWidth = Number(swatches[index], "width");
			var advance = Number(swatches[index + 1], "x") - Number(swatches[index], "x");

			advance.Should().BeGreaterThan(
				swatchWidth + (labels[index].Length * FontSize * 0.4),
				$"the entry for {labels[index]} has to clear its own label");
		}

		// And the row sits in the middle of the legend rather than starting at its edge.
		var first = Number(swatches[0], "x");
		var last = Number(swatches[^1], "x") + Number(swatches[^1], "width");

		first.Should().BeGreaterThan(0, "a centred row does not start hard against the edge");
		((first + last) / 2).Should().BeApproximately(
			Width / 2.0,
			Width * 0.12,
			"the packed row is centred, allowing for the label of the last entry not being measured");
	}

	/// <summary>
	/// The entry swatches of the legend.
	/// </summary>
	/// <remarks>
	/// The legend group carries a background rect of its own, which has no position and spans the
	/// whole legend, so the entries are the positioned rectangles narrower than the legend is.
	/// </remarks>
	private static List<XElement> SwatchesOf(XDocument document, double narrowerThan)
		=> [.. Elements(GroupById(document, "legend"), "rect")
			.Where(r => r.Attribute("x") is not null && r.Attribute("y") is not null)
			.Where(r => Number(r, "width") < narrowerThan)];
}
