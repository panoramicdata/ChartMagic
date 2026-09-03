using System.Drawing;
using System.Globalization;
using System.Xml.Linq;
using static PanoramicData.ChartMagic.Test.Support.RenderedChart;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// Tests for pie and doughnut rendering.
/// </summary>
/// <remarks>
/// A pie takes a different path through the renderer from everything else: no axes, no
/// gridlines, a wedge per point rather than a shape per series, and a legend describing slices.
/// The geometry assertions use the placement measured against DocMagic: a 65%-wide chart area
/// with an inner plot inset 10%, so an 800x400 image gives a 468x360 plot, and the pie is
/// centred in it at 0.95 of its shorter side. Coordinates are local to the inner plot group.
/// </remarks>
public class PieTests
{
	/// <summary>Half of the 468-wide inner plot.</summary>
	private const double CentreX = 234;

	/// <summary>Half of the 360-high inner plot.</summary>
	private const double CentreY = 180;

	/// <summary>0.95 * min(468, 360) / 2, the factor measured from DocMagic output.</summary>
	private const double Radius = 171;

	private static readonly string[] Quarters = ["Q1", "Q2", "Q3", "Q4"];

	private static readonly Color[] Colours =
		[Color.SteelBlue, Color.SeaGreen, Color.Goldenrod, Color.IndianRed];

	private static ChartSpecification PieChart(
		SeriesChartType chartType = SeriesChartType.Pie,
		double[]? values = null,
		PieLabelStyle labelStyle = PieLabelStyle.Inside)
	{
		values ??= [25, 25, 25, 25];

		return PieChartOf(
			chartType,
			labelStyle,
			[.. values.Select((value, index) => new ChartPoint(
				Quarters[index % Quarters.Length],
				index,
				value,
				Colours[index % Colours.Length]))]);
	}

	private static ChartSpecification PieChartOf(
		SeriesChartType chartType,
		PieLabelStyle labelStyle,
		List<ChartPoint> points)
		=> new()
		{
			SeriesList =
			[
				new()
				{
					ChartType = chartType,
					PieLabelStyle = labelStyle,
					Points = points
				}
			]
		};

	/// <summary>The pie group, or null where the chart drew none.</summary>
	private static XElement? PieGroup(ChartSpecification specification)
		=> FindGroupById(Render(specification), "pie");

	/// <summary>The <c>d</c> attribute of every wedge, in the order they are drawn.</summary>
	private static List<string> WedgePaths(ChartSpecification specification)
		=> [.. Elements(PieGroup(specification)!, "path").Select(path => path.Attribute("d")!.Value)];

	/// <summary>The <c>d</c> attribute of the first wedge.</summary>
	private static string FirstWedgePath(ChartSpecification specification)
		=> WedgePaths(specification)[0];

	/// <summary>The text drawn on or beside the slices.</summary>
	private static List<string> SliceLabels(ChartSpecification specification)
		=> [.. Elements(PieGroup(specification)!, "text").Select(text => text.Value)];

	/// <summary>
	/// The arc command an inner radius produces, as a fraction of the pie radius. A doughnut hole
	/// is what the ring does not occupy, so a ring of 60 leaves a hole of 0.4.
	/// </summary>
	private static string HoleArc(double holeFraction)
		=> FormattableString.Invariant($"A{Radius * holeFraction:F2} {Radius * holeFraction:F2}");

	/// <summary>A point on the pie's edge, at a clock position.</summary>
	private static string EdgePoint(double offsetX, double offsetY)
		=> FormattableString.Invariant($"{CentreX + offsetX:F2} {CentreY + offsetY:F2}");

	[Fact]
	public void PieChart_DrawsOneWedgePerPoint()
	{
		var pie = PieGroup(PieChart());

		pie.Should().NotBeNull("a pie chart draws into its own group");
		Elements(pie!, "path").Should().HaveCount(4, "one wedge per data point");
	}

	[Fact]
	public void PieChart_WedgesTakeTheColourOfTheirPoint()
	{
		Elements(PieGroup(PieChart())!, "path")
			.Select(w => w.Attribute("fill")!.Value)
			.Should()
			.Equal(["#4682B4", "#2E8B57", "#DAA520", "#CD5C5C"],
				"a pie is coloured per slice, not per series");
	}

	[Fact]
	public void PieChart_FirstWedgeStartsAtThreeOClockAndSweepsClockwise()
	{
		var first = FirstWedgePath(PieChart());

		// Four equal slices. Established by rendering quarters through both renderers: the
		// Microsoft chart control starts at three o'clock, so the first quarter runs from
		// there round to six o'clock.
		first.Should().StartWith(
			FormattableString.Invariant($"M{EdgePoint(0, 0)} L{EdgePoint(Radius, 0)}"),
			"a wedge is drawn from the centre out to the start of its arc, at three o'clock");

		first.Should().Contain(
			FormattableString.Invariant($"1 {EdgePoint(0, Radius)}"),
			"a quarter slice from three o'clock ends at the bottom, drawn clockwise");
	}

	[Fact]
	public void PieChart_SliceSizeFollowsTheValue()
	{
		// 50, 25, 25: the first slice is a half, so its arc is the only one flagged as large.
		var wedges = WedgePaths(PieChart(values: [50, 25, 25]));

		wedges.Should().HaveCount(3);

		// Starting at three o'clock, a half circle ends at nine.
		wedges[0].Should().Contain(EdgePoint(-Radius, 0));

		// And the next quarter ends at twelve.
		wedges[1].Should().Contain(EdgePoint(0, -Radius));
	}

	[Fact]
	public void PieChart_StartAngleRotatesTheWholePie()
	{
		var specification = PieChart();
		specification.PieStartAngleDegrees = 90;

		// Started a quarter turn on from three o'clock, the first slice begins at six.
		FirstWedgePath(specification).Should().StartWith(
			FormattableString.Invariant($"M{EdgePoint(0, 0)} L{EdgePoint(0, Radius)}"));
	}

	[Fact]
	public void PieChart_ASingleValueDrawsACompleteCircle()
	{
		// One slice sweeps the full 360 degrees, which cannot be drawn as a single arc because
		// its start and end points coincide.
		var wedges = WedgePaths(PieChart(values: [100]));

		wedges.Should().HaveCount(1);
		wedges[0].Split('A').Should().HaveCount(3, "a full circle is drawn as two half arcs");
		wedges[0].Should().NotContain("NaN");
	}

	[Fact]
	public void Doughnut_LeavesAHoleAtTheDefaultRadius()
	{
		var path = FirstWedgePath(PieChart(SeriesChartType.Doughnut));

		// A doughnut wedge is a band: out along one radius, round, back along the other. It never
		// visits the centre, which a pie wedge always does.
		path.Should().NotStartWith(FormattableString.Invariant($"M{EdgePoint(0, 0)}"));

		// The default radius of 60 is the width of the RING, so the hole is the other 40% - not
		// 60%, as this asserted while the renderer made the same mistake. Measured against the
		// reference render: at 285 pixels across, its default doughnut left a hole 116 wide, which
		// is 40.7%.
		path.Should().Contain(
			HoleArc(0.4),
			"the inner arc runs at the hole radius, which is what the ring does not occupy");
	}

	/// <summary>
	/// A narrower ring leaves a bigger hole, whether the radius arrives as a number or a string.
	/// </summary>
	/// <remarks>
	/// Measured: asking the reference renderer for 30 left a hole 202 of 285 pixels wide, or
	/// 70.9%. Reading the number as the hole itself inverted the shape - a request for a thin ring
	/// drew a fat one.
	///
	/// The specification declares DoughnutRadius as an object because the corresponding Microsoft
	/// chart custom property is a string, and callers pass either, so both are exercised here.
	/// </remarks>
	[Theory]
	[InlineData(30d)]
	[InlineData("30")]
	public void Doughnut_NarrowerRingLeavesABiggerHole(object radius)
	{
		var specification = PieChart(SeriesChartType.Doughnut);
		specification.DoughnutRadius = radius;

		FirstWedgePath(specification).Should().Contain(HoleArc(0.7));
	}

	[Fact]
	public void PieChart_DrawsNoAxes()
	{
		var document = Render(PieChart());

		FindGroupById(document, "xAxis").Should().BeNull("a pie has no axes");
		FindGroupById(document, "yAxis").Should().BeNull("a pie has no axes");
		FindGroupById(document, "gridlines").Should().BeNull("nor gridlines");
	}

	[Fact]
	public void PieChart_LegendNamesEverySlice()
	{
		var document = Render(PieChart());
		var legend = FindGroupById(document, "legend");

		legend.Should().NotBeNull();
		LabelTexts(document, "legend").Should().Equal(Quarters,
			"a pie legend describes slices, not series");
		Elements(legend!, "rect").Should().HaveCount(
			5,
			"one swatch per slice, plus the legend background");
	}

	[Fact]
	public void PieChart_LabelsShowTheCategoryNameByDefault()
	{
		// Measured against DocMagic: with no label text set, the Microsoft chart control labels a
		// pie slice with its X value, so the names appear rather than the numbers.
		SliceLabels(PieChart(values: [10, 20, 30, 40])).Should().Equal(Quarters);
	}

	[Fact]
	public void PieChart_LabelStyleDisabledDrawsNoLabels()
	{
		var specification = PieChart(labelStyle: PieLabelStyle.Disabled);

		SliceLabels(specification).Should().BeEmpty();
		WedgePaths(specification).Should().HaveCount(4, "the slices are still drawn");
	}

	/// <summary>
	/// Labels outside the pie are placed clear of the edge, with no leader lines.
	/// </summary>
	/// <remarks>
	/// This asserted a leader line per label. The renderer this matches draws none - the labels
	/// sit just clear of the edge, so there is nothing for a line to bridge - and it does not
	/// shrink the pie to make room for them either: measured at 283 pixels across with outside
	/// labels against 285 with inside ones, where shrinking gave 225.
	/// </remarks>
	[Fact]
	public void PieChart_LabelStyleOutside_PlacesLabelsBeyondTheEdgeWithoutLeaderLines()
	{
		var pie = PieGroup(PieChart(labelStyle: PieLabelStyle.Outside))!;

		Elements(pie, "line").Should().BeEmpty("the reference renderer draws no leader lines");
		Elements(pie, "text").Should().HaveCount(4);

		// Every label outside the slices, which is what "outside" has to mean if the pie is not
		// shrunk to make room.
		var wedge = Elements(pie, "path")[0].Attribute("d")!.Value;
		var pieRadius = double.Parse(
			wedge.Split('A')[1].Trim().Split(' ')[0],
			CultureInfo.InvariantCulture);

		Elements(pie, "text").Should().AllSatisfy(text =>
			DistanceFromCentre(text).Should().BeGreaterThan(
				pieRadius,
				"a label placed outside the pie is further from the centre than the edge is"));
	}

	[Fact]
	public void PieChart_LabelKeywordsAreSubstituted()
	{
		var specification = PieChart(values: [25, 25, 25, 25]);
		specification.SeriesList[0].LabelText = "#VALX: #PERCENT";

		SliceLabels(specification)
			.Should()
			.Equal(
				["Q1: 25.00%", "Q2: 25.00%", "Q3: 25.00%", "Q4: 25.00%"],
				"#PERCENT carries two decimal places, as it does in the renderer this matches - a "
					+ "reference render of the same chart showed 34.00%, 26.00% and 18.00%");
	}

	/// <summary>
	/// Slices below the threshold are combined into one - unless there is only one of them, in
	/// which case replacing it with a combined slice of the same size would hide its identity and
	/// gain nothing, so it is left alone.
	/// </summary>
	[Theory]
	[InlineData(new[] { 60d, 30, 5, 5 }, "Everything else")]
	[InlineData(new[] { 60d, 35, 5 }, "Q3")]
	public void PieChart_CollectsSeveralSlicesBelowTheThresholdButNotOne(
		double[] values,
		string expectedLastLegendEntry)
	{
		var specification = PieChart(values: values);
		specification.PieCollectedThresholdPercent = 10;
		specification.PieCollectedLabel = "Everything else";

		// Either way there are three wedges: two collected into one, or three left as they are.
		WedgePaths(specification).Should().HaveCount(3);
		LabelTexts(Render(specification), "legend")
			.Should()
			.Equal(["Q1", "Q2", expectedLastLegendEntry]);
	}

	[Fact]
	public void PieChart_IgnoresPointsWithNoValue()
	{
		var specification = PieChartOf(
			SeriesChartType.Pie,
			PieLabelStyle.Inside,
			[
				new("Q1", 0, 50, Color.SteelBlue),
				new("Q2", 1, null, Color.SeaGreen),
				new("Q3", 2, 50, Color.Goldenrod)
			]);

		WedgePaths(specification).Should().HaveCount(2, "a point with no value has no slice");
	}

	[Fact]
	public void PieChart_WithNoUsableValuesDrawsNothingRatherThanThrowing()
	{
		var specification = PieChartOf(
			SeriesChartType.Pie,
			PieLabelStyle.Inside,
			[new("Q1", 0, 0), new("Q2", 1, null)]);

		Elements(PieGroup(specification) ?? new XElement("empty"), "path").Should().BeEmpty();
	}

	[Fact]
	public void PieChart_WedgeCoordinatesAreCultureIndependent()
	{
		// A comma-decimal culture would produce a path no SVG parser can read.
		FirstWedgePath(PieChart(values: [33, 33, 34]))
			.Should()
			.MatchRegex(@"^M[\d.]+ [\d.]+ L[\d.]+ [\d.]+ A[\d.]+ [\d.]+ 0 [01] 1 [\d.]+ [\d.]+ Z$");
	}

	/// <summary>
	/// How far a label sits from the centre of the pie.
	/// </summary>
	private static double DistanceFromCentre(XElement text)
	{
		var x = Number(text, "x");
		var y = Number(text, "y");
		return Math.Sqrt(Math.Pow(x - CentreX, 2) + Math.Pow(y - CentreY, 2));
	}
}
