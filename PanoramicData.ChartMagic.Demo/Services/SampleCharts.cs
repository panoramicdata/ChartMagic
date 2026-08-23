using PanoramicData.ChartMagic.Extensions;
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
/// The colours a chart needs in order to sit on a light or a dark page.
/// </summary>
/// <param name="AxisLine">Axis lines and tick marks.</param>
/// <param name="AxisLabel">Tick labels, axis titles and legend text.</param>
/// <param name="MajorGrid">Major gridlines.</param>
/// <param name="MinorGrid">Minor gridlines.</param>
/// <param name="Border">The chart border.</param>
public record ChartTheme(Color AxisLine, Color AxisLabel, Color MajorGrid, Color MinorGrid, Color Border)
{
	/// <summary>For a light page.</summary>
	public static ChartTheme Light { get; } = new(
		AxisLine: Color.FromArgb(0x59, 0x59, 0x59),
		AxisLabel: Color.FromArgb(0x33, 0x33, 0x33),
		MajorGrid: Color.FromArgb(0xCC, 0xCC, 0xCC),
		MinorGrid: Color.FromArgb(0xE8, 0xE8, 0xE8),
		Border: Color.FromArgb(0xB0, 0xB0, 0xB0));

	/// <summary>For a dark page.</summary>
	public static ChartTheme Dark { get; } = new(
		AxisLine: Color.FromArgb(0xB8, 0xBC, 0xC2),
		AxisLabel: Color.FromArgb(0xE6, 0xE8, 0xEB),
		MajorGrid: Color.FromArgb(0x4A, 0x4F, 0x57),
		MinorGrid: Color.FromArgb(0x35, 0x39, 0x40),
		Border: Color.FromArgb(0x5A, 0x60, 0x68));
}

/// <summary>
/// The sample gallery, and the SVG rendering used to display it.
/// </summary>
public static class SampleCharts
{
	private const int Width = 720;
	private const int Height = 380;

	/// <summary>
	/// A deliberately translucent chart background: 20% grey, so the page shows through it.
	/// </summary>
	/// <remarks>
	/// This is the demo's own check on issue #35. Element opacity used to be used for a
	/// translucent fill, which faded the border along with it; a chart that is translucent
	/// inside and cleanly framed outside can only be drawn once fill-opacity is used instead.
	/// The container behind every chart is striped, so anything opaque is obvious at a glance.
	/// </remarks>
	private static readonly Color TranslucentBackground = Color.FromArgb(0x33, 0x77, 0x77, 0x77);

	private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

	/// <summary>
	/// Renders a specification to an inline SVG string, in the colours of the given theme.
	/// </summary>
	/// <remarks>
	/// SVG rather than PNG, because this runs in the browser under WebAssembly. The raster path
	/// needs the SkiaSharp native library; the SVG path does not touch it, so the demo stays a
	/// pure static site with no native assets to ship.
	///
	/// The colours are baked into the SVG rather than inherited from the page, so each sample is
	/// rendered once per theme and the stylesheet shows whichever matches. A single render
	/// cannot follow the reader's colour scheme, because the renderer writes concrete colours
	/// into every attribute.
	/// </remarks>
	public static string ToSvg(ChartSpecification specification, ChartTheme theme)
	{
		Apply(specification, theme);

		using var stream = new MemoryStream();
		specification
			.ToChart()
			.SaveImage(stream, ChartImageFormat.Svg, Width, Height);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private static void Apply(ChartSpecification specification, ChartTheme theme)
	{
		specification.ChartBackgroundColor = TranslucentBackground;
		specification.ChartBorderColor = theme.Border;
		specification.ChartBorderWidth = 1;

		specification.XAxisLineColor = theme.AxisLine;
		specification.YAxisLineColor = theme.AxisLine;
		specification.XAxisMajorGridColor = theme.MajorGrid;
		specification.YAxisMajorGridColor = theme.MajorGrid;
		specification.XAxisMinorGridColor = theme.MinorGrid;
		specification.YAxisMinorGridColor = theme.MinorGrid;
		specification.AxisLabelColor = theme.AxisLabel;
		specification.LegendFontColor = theme.AxisLabel;

		// The default 20px axis text is too large for a 720px-wide sample.
		specification.XAxisFontSize = 12;
		specification.YAxisFontSize = 12;
		specification.LegendFontSize = 13;
	}

	/// <summary>
	/// The gallery, ordered so that what works comes first and the gaps are visible below it.
	/// </summary>
	public static IReadOnlyList<ChartSample> All =>
	[
		new(
			"Column",
			"Three column series, grouped side by side within each category, with the category "
			+ "labels taken from the data and a value axis generated from the range. Issue #33.",
			SampleStatus.Working,
			WithAxes(BuildMultiSeries(SeriesChartType.Column), "Day", "Percent")),

		new(
			"Stacked column",
			"The same three series stacked. Each segment starts where the one below it ends, and "
			+ "the axis is scaled to the stacked total rather than the largest single value.",
			SampleStatus.Working,
			WithAxes(BuildMultiSeries(SeriesChartType.StackedColumn), "Day", "Percent")),

		new(
			"Bar",
			"Horizontal bars. The category axis moves to the left and the value axis along the "
			+ "bottom, so the same specification reads sideways.",
			SampleStatus.Working,
			WithAxes(BuildMultiSeries(SeriesChartType.Bar), "Percent", "Day")),

		new(
			"Axis titles, gridlines and rotated labels",
			"Axis titles on both axes, major and minor gridlines, and category labels rotated "
			+ "45 degrees. All four were accepted and silently discarded before issue #31.",
			SampleStatus.Working,
			BuildWithAxisFurniture()),

		new(
			"Logarithmic Y axis",
			"Values spanning four orders of magnitude. The axis is labelled in whole decades and "
			+ "the small values are legible instead of being flattened against the floor.",
			SampleStatus.Working,
			BuildLogarithmic()),

		new(
			"Line with markers",
			"A single line series with circle markers, over a translucent chart background - the "
			+ "stripes behind it are the page, showing through.",
			SampleStatus.Working,
			WithAxes(Build(SeriesChartType.Line, markers: true), "Day", "Percent")),

		new(
			"Area",
			"Area fill under a single series.",
			SampleStatus.Working,
			WithAxes(Build(SeriesChartType.Area), "Day", "Percent")),

		new(
			"Stacked area",
			"Three series stacked, each outlined and filled.",
			SampleStatus.Working,
			WithAxes(BuildMultiSeries(SeriesChartType.StackedArea), "Day", "Percent")),

		new(
			"Hundred-percent stacked column",
			"Deliberately still blank. Rendering these needs the value axis rescaled to 0-100 "
			+ "per category, which is not wired up, and a chart showing plausible but wrong "
			+ "proportions would be worse than one showing nothing. Issue #33.",
			SampleStatus.NotImplemented,
			WithAxes(BuildMultiSeries(SeriesChartType.StackedColumn100), "Day", "Percent")),

		new(
			"Pie",
			"One series, one slice per point, coloured per point. Angles run clockwise from "
			+ "twelve o'clock, and the labels show each value.",
			SampleStatus.Working,
			BuildPie(SeriesChartType.Pie)),

		new(
			"Doughnut",
			"The same data as a ring. The hole is 60% of the radius, matching the Microsoft "
			+ "chart control default, and is configurable.",
			SampleStatus.Working,
			BuildPie(SeriesChartType.Doughnut)),

		new(
			"Pie with outside labels and a collected slice",
			"Labels outside on leader lines, showing each slice as a percentage. The two "
			+ "smallest slices fall below a 15% threshold and are combined into one.",
			SampleStatus.Working,
			BuildCollectedPie()),
	];

	private static List<ChartPoint> Points(params double[] values)
	{
		var points = new List<ChartPoint>();
		for (var i = 0; i < values.Length; i++)
		{
			// The label makes the axis categorical; the index positions it.
			points.Add(new ChartPoint(Days[i % Days.Length], i, values[i]));
		}

		return points;
	}

	private static ChartSpecification WithAxes(ChartSpecification specification, string xTitle, string yTitle)
	{
		specification.XAxisTitle = xTitle;
		specification.YAxisTitle = yTitle;
		specification.YAxisMajorGridEnabled = true;
		return specification;
	}

	private static ChartSpecification Build(SeriesChartType chartType, bool markers = false)
		=> new()
		{
			SeriesList =
			[
				new()
				{
					ChartType = chartType,
					LegendText = "Utilisation",
					StrokeColor = Color.SteelBlue,
					FillColor = chartType == SeriesChartType.Line ? Colors.Transparent : Color.LightSteelBlue,
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

		var specification = new ChartSpecification();
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

		// Stacked series read better as a solid block than as three outlined ones.
		if (chartType == SeriesChartType.StackedArea)
		{
			specification.LegendStyle = LegendStyle.Column;
		}

		return specification;
	}

	private static ChartSpecification BuildPie(SeriesChartType chartType)
	{
		var colours = new[]
		{
			Color.SteelBlue,
			Color.SeaGreen,
			Color.Goldenrod,
			Color.IndianRed,
			Color.MediumPurple
		};
		var names = new[] { "London", "Manchester", "Leeds", "Bristol", "Glasgow" };
		var values = new double[] { 34, 26, 18, 13, 9 };

		return new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = chartType,
					StrokeColor = Color.White,
					StrokeWidth = 1,
					Points =
					[
						.. values.Select((value, index) => new ChartPoint(
							names[index],
							index,
							value,
							colours[index]))
					]
				}
			]
		};
	}

	private static ChartSpecification BuildCollectedPie()
	{
		var specification = BuildPie(SeriesChartType.Pie);
		specification.PieLabelStyle = "Outside";
		specification.PieCollectedThresholdPercent = 15;
		specification.PieCollectedLabel = "Other";
		specification.PieCollectedColor = Color.DarkGray;
		specification.SeriesList[0].LabelText = "#PERCENT";
		return specification;
	}

	private static ChartSpecification BuildWithAxisFurniture()
	{
		var specification = WithAxes(Build(SeriesChartType.Line, markers: true), "Day", "Percent");
		specification.XAxisMajorGridEnabled = true;
		specification.YAxisMinorGridEnabled = true;
		specification.XAxisLabelAngle = -45;
		specification.LegendStyle = LegendStyle.Column;
		return specification;
	}

	private static ChartSpecification BuildLogarithmic()
	{
		var specification = new ChartSpecification
		{
			YAxisIsLogarithmic = true,
			YAxisMajorGridEnabled = true,
			YAxisMinorGridEnabled = true,
			XAxisTitle = "Day",
			YAxisTitle = "Requests",
			UseYAxisShortLabels = true,
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					LegendText = "Requests",
					StrokeColor = Color.IndianRed,
					StrokeWidth = 3,
					IsXValueIndexed = true,
					MarkerStyle = MarkerStyle.Circle,
					MarkerFillColor = Color.White,
					MarkerStrokeColor = Color.IndianRed,
					MarkerSize = 4,
					Points = Points(1, 9, 80, 700, 5000, 900, 40)
				}
			]
		};

		return specification;
	}
}
