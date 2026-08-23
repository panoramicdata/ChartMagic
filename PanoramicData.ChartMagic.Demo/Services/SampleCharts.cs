using PanoramicData.ChartMagic.Models;
using System.Drawing;
using System.Text;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// A sample and its current rendering status.
/// </summary>
/// <param name="Title">Short name for the sample.</param>
/// <param name="Notes">What the sample is meant to show.</param>
/// <param name="Status">Whether it currently renders as intended.</param>
/// <param name="Specification">The chart to render.</param>
public record ChartSample(string Title, string Notes, SampleStatus Status, ChartSpecification Specification);

/// <summary>
/// How close a sample is to rendering correctly. The demo is a scoreboard as much as a gallery.
/// </summary>
public enum SampleStatus
{
	/// <summary>Renders as intended.</summary>
	Working,

	/// <summary>Renders, but not everything asked for appears.</summary>
	Partial,

	/// <summary>Draws nothing, or nothing useful.</summary>
	NotImplemented
}

/// <summary>
/// The sample gallery, and the SVG rendering used to display it.
/// </summary>
public static class SampleCharts
{
	private const int Width = 720;
	private const int Height = 380;

	/// <summary>
	/// Renders a specification to an inline SVG string.
	/// </summary>
	/// <remarks>
	/// SVG rather than PNG, because this runs in the browser under WebAssembly. The raster path
	/// needs the SkiaSharp native library; the SVG path does not touch it, so the demo stays a
	/// pure static site with no native assets to ship.
	/// </remarks>
	public static string ToSvg(ChartSpecification specification)
	{
		using var stream = new MemoryStream();
		specification
			.ToChart()
			.SaveImage(stream, ChartImageFormat.Svg, Width, Height);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>
	/// The gallery, ordered so that what works comes first and the gaps are visible below it.
	/// </summary>
	public static IReadOnlyList<ChartSample> All =>
	[
		new(
			"Line with markers",
			"A single line series with circle markers. The workhorse case, and the one that works today.",
			SampleStatus.Working,
			Build(SeriesChartType.Line, markers: true)),

		new(
			"Area",
			"Area fill under a single series.",
			SampleStatus.Working,
			Build(SeriesChartType.Area)),

		new(
			"Stacked area",
			"Three series stacked. Renders correctly.",
			SampleStatus.Working,
			BuildMultiSeries(SeriesChartType.StackedArea)),

		new(
			"Column",
			"Three column series. Nothing is drawn: InternalSvgRenderer has no case for Column. "
			+ "This is issue #33 and the single biggest gap for business reporting.",
			SampleStatus.NotImplemented,
			BuildMultiSeries(SeriesChartType.Column)),

		new(
			"Bar",
			"Horizontal bars. Also unimplemented (#33).",
			SampleStatus.NotImplemented,
			BuildMultiSeries(SeriesChartType.Bar)),

		new(
			"Axis titles and gridlines",
			"A line series asking for axis titles, major gridlines and a rotated label angle. "
			+ "The series draws; none of the axis furniture does. Issue #31.",
			SampleStatus.Partial,
			BuildWithAxisFurniture()),

		new(
			"Logarithmic Y axis",
			"Values spanning three orders of magnitude with IsLogarithmic set. The flag is "
			+ "accepted and ignored, so the small values are flattened against the axis. Issue #31.",
			SampleStatus.Partial,
			BuildLogarithmic())
	];

	private static List<ChartPoint> Points(params double[] values)
	{
		var points = new List<ChartPoint>();
		for (var i = 0; i < values.Length; i++)
		{
			points.Add(new ChartPoint(null, i + 1, values[i]));
		}

		return points;
	}

	private static ChartSpecification Build(SeriesChartType chartType, bool markers = false)
		=> new()
		{
			ChartBackgroundColor = Color.White,
			SeriesList =
			[
				new()
				{
					ChartType = chartType,
					LegendText = "Utilisation",
					StrokeColor = Color.SteelBlue,
					FillColor = Color.LightSteelBlue,
					StrokeWidth = 3,
					IsXValueIndexed = true,
					MarkerStyle = markers ? MarkerStyle.Circle : MarkerStyle.None,
					MarkerFillColor = markers ? Color.White : null,
					MarkerStrokeColor = markers ? Color.SteelBlue : null,
					MarkerSize = markers ? 4 : null,
					Points = Points(12, 19, 14, 22, 26, 21, 30)
				}
			]
		};

	private static ChartSpecification BuildMultiSeries(SeriesChartType chartType)
	{
		var colours = new[] { Color.SteelBlue, Color.SeaGreen, Color.Goldenrod };
		var names = new[] { "CPU", "Memory", "Disk" };
		var data = new[]
		{
			new double[] { 12, 19, 14, 22, 26, 21, 30 },
			new double[] { 8, 11, 9, 14, 16, 15, 18 },
			new double[] { 4, 6, 5, 7, 9, 8, 11 }
		};

		var specification = new ChartSpecification { ChartBackgroundColor = Color.White };
		for (var i = 0; i < names.Length; i++)
		{
			specification.SeriesList.Add(new SeriesSpecification
			{
				ChartType = chartType,
				LegendText = names[i],
				StrokeColor = colours[i],
				FillColor = colours[i],
				StrokeWidth = 1,
				IsXValueIndexed = true,
				Points = Points(data[i])
			});
		}

		return specification;
	}

	private static ChartSpecification BuildWithAxisFurniture()
	{
		var specification = Build(SeriesChartType.Line, markers: true);
		specification.XAxisTitle = "Day";
		specification.YAxisTitle = "Percent";
		specification.YAxisMajorGridEnabled = true;
		specification.XAxisMajorGridEnabled = true;
		specification.XAxisLabelAngle = 45;
		return specification;
	}

	private static ChartSpecification BuildLogarithmic()
	{
		var specification = new ChartSpecification
		{
			ChartBackgroundColor = Color.White,
			YAxisIsLogarithmic = true,
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					LegendText = "Requests",
					StrokeColor = Color.IndianRed,
					StrokeWidth = 3,
					IsXValueIndexed = true,
					Points = Points(1, 9, 80, 700, 5000, 900, 40)
				}
			]
		};

		return specification;
	}
}
