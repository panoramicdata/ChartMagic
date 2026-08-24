using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

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
	private const int Width = 800;
	private const int Height = 400;

	/// <summary>Half of the 468-wide inner plot.</summary>
	private const double CentreX = 234;

	/// <summary>Half of the 360-high inner plot.</summary>
	private const double CentreY = 180;

	/// <summary>0.95 * min(468, 360) / 2, the factor measured from DocMagic output.</summary>
	private const double Radius = 171;

	private static readonly string[] Quarters = ["Q1", "Q2", "Q3", "Q4"];

	private static XDocument Render(ChartSpecification specification)
	{
		using var stream = new MemoryStream();
		specification.ToChart().SaveImage(stream, ChartImageFormat.Svg, Width, Height);
		return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
	}

	private static XElement? GroupById(XDocument document, string id)
		=> document
			.Descendants()
			.FirstOrDefault(e => e.Name.LocalName == "g" && e.Attribute("id")?.Value == id);

	private static List<XElement> Elements(XElement parent, string localName)
		=> parent.Descendants().Where(e => e.Name.LocalName == localName).ToList();

	private static ChartSpecification PieChart(
		SeriesChartType chartType = SeriesChartType.Pie,
		double[]? values = null,
		PieLabelStyle labelStyle = PieLabelStyle.Inside)
	{
		var colours = new[] { Color.SteelBlue, Color.SeaGreen, Color.Goldenrod, Color.IndianRed };
		values ??= [25, 25, 25, 25];

		return new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = chartType,
					PieLabelStyle = labelStyle,
					Points =
					[
						.. values.Select((value, index) => new ChartPoint(
							Quarters[index % Quarters.Length],
							index,
							value,
							colours[index % colours.Length]))
					]
				}
			]
		};
	}

	[Fact]
	public void PieChart_DrawsOneWedgePerPoint()
	{
		var pie = GroupById(Render(PieChart()), "pie");

		pie.Should().NotBeNull("a pie chart draws into its own group");
		Elements(pie!, "path").Should().HaveCount(4, "one wedge per data point");
	}

	[Fact]
	public void PieChart_WedgesTakeTheColourOfTheirPoint()
	{
		var wedges = Elements(GroupById(Render(PieChart()), "pie")!, "path");

		wedges.Select(w => w.Attribute("fill")!.Value)
			.Should()
			.Equal(["#4682B4", "#2E8B57", "#DAA520", "#CD5C5C"],
				"a pie is coloured per slice, not per series");
	}

	[Fact]
	public void PieChart_FirstWedgeStartsAtThreeOClockAndSweepsClockwise()
	{
		var first = Elements(GroupById(Render(PieChart()), "pie")!, "path")[0]
			.Attribute("d")!
			.Value;

		// Four equal slices. Established by rendering quarters through both renderers: the
		// Microsoft chart control starts at three o'clock, so the first quarter runs from
		// there round to six o'clock.
		first.Should().StartWith(
			FormattableString.Invariant($"M{CentreX:F2} {CentreY:F2} L{CentreX + Radius:F2} {CentreY:F2}"),
			"a wedge is drawn from the centre out to the start of its arc, at three o'clock");

		first.Should().Contain(
			FormattableString.Invariant($"1 {CentreX:F2} {CentreY + Radius:F2}"),
			"a quarter slice from three o'clock ends at the bottom, drawn clockwise");
	}

	[Fact]
	public void PieChart_SliceSizeFollowsTheValue()
	{
		// 50, 25, 25: the first slice is a half, so its arc is the only one flagged as large.
		var wedges = Elements(GroupById(Render(PieChart(values: [50, 25, 25])), "pie")!, "path")
			.Select(w => w.Attribute("d")!.Value)
			.ToList();

		wedges.Should().HaveCount(3);

		// Starting at three o'clock, a half circle ends at nine.
		wedges[0].Should().Contain(FormattableString.Invariant($"{CentreX - Radius:F2} {CentreY:F2}"));

		// And the next quarter ends at twelve.
		wedges[1].Should().Contain(FormattableString.Invariant($"{CentreX:F2} {CentreY - Radius:F2}"));
	}

	[Fact]
	public void PieChart_StartAngleRotatesTheWholePie()
	{
		var specification = PieChart();
		specification.PieStartAngleDegrees = 90;

		var first = Elements(GroupById(Render(specification), "pie")!, "path")[0].Attribute("d")!.Value;

		// Started a quarter turn on from three o'clock, the first slice begins at six.
		first.Should().StartWith(
			FormattableString.Invariant($"M{CentreX:F2} {CentreY:F2} L{CentreX:F2} {CentreY + Radius:F2}"));
	}

	[Fact]
	public void PieChart_ASingleValueDrawsACompleteCircle()
	{
		// One slice sweeps the full 360 degrees, which cannot be drawn as a single arc because
		// its start and end points coincide.
		var wedges = Elements(GroupById(Render(PieChart(values: [100])), "pie")!, "path");

		wedges.Should().HaveCount(1);

		var path = wedges[0].Attribute("d")!.Value;
		path.Split('A').Should().HaveCount(3, "a full circle is drawn as two half arcs");
		path.Should().NotContain("NaN");
	}

	[Fact]
	public void Doughnut_LeavesAHoleAtTheDefaultRadius()
	{
		var path = Elements(GroupById(Render(PieChart(SeriesChartType.Doughnut)), "pie")!, "path")[0]
			.Attribute("d")!
			.Value;

		// A doughnut wedge is a band: out along one radius, round, back along the other. It never
		// visits the centre, which a pie wedge always does.
		path.Should().NotStartWith(FormattableString.Invariant($"M{CentreX:F2} {CentreY:F2}"));

		// The default radius of 60 is the width of the RING, so the hole is the other 40% - not
		// 60%, as this asserted while the renderer made the same mistake. Measured against the
		// reference render: at 285 pixels across, its default doughnut left a hole 116 wide, which
		// is 40.7%.
		path.Should().Contain(
			FormattableString.Invariant($"A{Radius * 0.4:F2} {Radius * 0.4:F2}"),
			"the inner arc runs at the hole radius, which is what the ring does not occupy");
	}

	/// <summary>
	/// A narrower ring leaves a bigger hole.
	/// </summary>
	/// <remarks>
	/// Measured: asking the reference renderer for 30 left a hole 202 of 285 pixels wide, or
	/// 70.9%. Reading the number as the hole itself inverted the shape - a request for a thin ring
	/// drew a fat one.
	/// </remarks>
	[Fact]
	public void Doughnut_NarrowerRingLeavesABiggerHole()
	{
		var specification = PieChart(SeriesChartType.Doughnut);
		specification.DoughnutRadius = 30;

		Elements(GroupById(Render(specification), "pie")!, "path")[0]
			.Attribute("d")!
			.Value
			.Should()
			.Contain(FormattableString.Invariant($"A{Radius * 0.7:F2} {Radius * 0.7:F2}"));
	}

	[Fact]
	public void Doughnut_AcceptsTheRadiusAsAString()
	{
		// The specification declares DoughnutRadius as an object because the corresponding
		// Microsoft chart custom property is a string, and callers pass either.
		var specification = PieChart(SeriesChartType.Doughnut);
		specification.DoughnutRadius = "30";

		Elements(GroupById(Render(specification), "pie")!, "path")[0]
			.Attribute("d")!
			.Value
			.Should()
			.Contain(FormattableString.Invariant($"A{Radius * 0.7:F2} {Radius * 0.7:F2}"));
	}

	[Fact]
	public void PieChart_DrawsNoAxes()
	{
		var document = Render(PieChart());

		GroupById(document, "xAxis").Should().BeNull("a pie has no axes");
		GroupById(document, "yAxis").Should().BeNull("a pie has no axes");
		GroupById(document, "gridlines").Should().BeNull("nor gridlines");
	}

	[Fact]
	public void PieChart_LegendNamesEverySlice()
	{
		var legend = GroupById(Render(PieChart()), "legend");

		legend.Should().NotBeNull();
		Elements(legend!, "text").Select(t => t.Value).Should().Equal(Quarters,
			"a pie legend describes slices, not series");
		Elements(legend!, "rect").Should().HaveCount(
			5,
			"one swatch per slice, plus the legend background");
	}

	[Fact]
	public void PieChart_LabelsShowTheCategoryNameByDefault()
	{
		var labels = Elements(GroupById(Render(PieChart(values: [10, 20, 30, 40]))!, "pie")!, "text")
			.Select(t => t.Value)
			.ToList();

		// Measured against DocMagic: with no label text set, the Microsoft chart control labels a
		// pie slice with its X value, so the names appear rather than the numbers.
		labels.Should().Equal(Quarters);
	}

	[Fact]
	public void PieChart_LabelStyleDisabledDrawsNoLabels()
	{
		var pie = GroupById(Render(PieChart(labelStyle: PieLabelStyle.Disabled)), "pie");

		Elements(pie!, "text").Should().BeEmpty();
		Elements(pie!, "path").Should().HaveCount(4, "the slices are still drawn");
	}

	[Fact]
	public void PieChart_LabelStyleOutsideAddsALeaderLinePerSlice()
	{
		var pie = GroupById(Render(PieChart(labelStyle: PieLabelStyle.Outside)), "pie");

		Elements(pie!, "line").Should().HaveCount(4, "one leader line per label");
		Elements(pie!, "text").Should().HaveCount(4);
	}

	[Fact]
	public void PieChart_LabelKeywordsAreSubstituted()
	{
		var specification = PieChart(values: [25, 25, 25, 25]);
		specification.SeriesList[0].LabelText = "#VALX: #PERCENT";

		Elements(GroupById(Render(specification), "pie")!, "text")
			.Select(t => t.Value)
			.Should()
			.Equal(["Q1: 25%", "Q2: 25%", "Q3: 25%", "Q4: 25%"]);
	}

	[Fact]
	public void PieChart_CollectsSlicesBelowTheThreshold()
	{
		var specification = PieChart(values: [60, 30, 5, 5]);
		specification.PieCollectedThresholdPercent = 10;
		specification.PieCollectedLabel = "Everything else";

		var document = Render(specification);

		// The two 5% slices become one, so three wedges rather than four.
		Elements(GroupById(document, "pie")!, "path").Should().HaveCount(3);
		Elements(GroupById(document, "legend")!, "text")
			.Select(t => t.Value)
			.Should()
			.Equal(["Q1", "Q2", "Everything else"]);
	}

	[Fact]
	public void PieChart_DoesNotCollectASingleSmallSlice()
	{
		// Replacing one slice with a combined slice of the same size hides its identity and
		// gains nothing, so it is left alone.
		var specification = PieChart(values: [60, 35, 5]);
		specification.PieCollectedThresholdPercent = 10;

		var document = Render(specification);

		Elements(GroupById(document, "pie")!, "path").Should().HaveCount(3);
		Elements(GroupById(document, "legend")!, "text")
			.Select(t => t.Value)
			.Should()
			.Equal(["Q1", "Q2", "Q3"]);
	}

	[Fact]
	public void PieChart_IgnoresPointsWithNoValue()
	{
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Pie,
					Points =
					[
						new("Q1", 0, 50, Color.SteelBlue),
						new("Q2", 1, null, Color.SeaGreen),
						new("Q3", 2, 50, Color.Goldenrod)
					]
				}
			]
		};

		Elements(GroupById(Render(specification), "pie")!, "path")
			.Should()
			.HaveCount(2, "a point with no value has no slice");
	}

	[Fact]
	public void PieChart_WithNoUsableValuesDrawsNothingRatherThanThrowing()
	{
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Pie,
					Points = [new("Q1", 0, 0), new("Q2", 1, null)]
				}
			]
		};

		var document = Render(specification);

		Elements(GroupById(document, "pie") ?? new XElement("empty"), "path").Should().BeEmpty();
	}

	[Fact]
	public void PieChart_WedgeCoordinatesAreCultureIndependent()
	{
		// A comma-decimal culture would produce a path no SVG parser can read.
		var path = Elements(GroupById(Render(PieChart(values: [33, 33, 34])), "pie")!, "path")[0]
			.Attribute("d")!
			.Value;

		path.Should().MatchRegex(@"^M[\d.]+ [\d.]+ L[\d.]+ [\d.]+ A[\d.]+ [\d.]+ 0 [01] 1 [\d.]+ [\d.]+ Z$");
	}
}
