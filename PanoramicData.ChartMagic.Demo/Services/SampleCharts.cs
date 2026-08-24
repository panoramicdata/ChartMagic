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
	/// The size every sample is rendered at, so a comparison can ask for the same one.
	/// </summary>
	public static int WidthPixels => Width;

	/// <inheritdoc cref="WidthPixels"/>
	public static int HeightPixels => Height;

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
		// Themed on a copy. The caller owns this specification and may be editing it live, so
		// writing theme colours into it would show them back as though the user had set them.
		var themed = SpecificationEditor.Clone(specification);
		Apply(themed, theme);

		using var stream = new MemoryStream();
		themed
			.ToChart()
			.SaveImage(stream, ChartImageFormat.Svg, Width, Height);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>
	/// Fills in the theme colours and the sample sizing, without overwriting anything already set.
	/// </summary>
	/// <remarks>
	/// Every assignment is conditional on the property still holding its default, and that is the
	/// point: these were written unconditionally, so editing XAxisFontSize on the page appeared to
	/// do nothing - the edit was made and then overwritten at render time by the theme. The same
	/// silently defeated every colour and the plot geometry. A sample that sets one of these
	/// deliberately now keeps it too, which it did not before.
	/// </remarks>
	private static void Apply(ChartSpecification specification, ChartTheme theme)
	{
		SetIfDefault(specification, nameof(ChartSpecification.ChartBackgroundColor), TranslucentBackground);
		SetIfDefault(specification, nameof(ChartSpecification.ChartBorderColor), theme.Border);
		SetIfDefault(specification, nameof(ChartSpecification.ChartBorderWidth), 1);
		SetIfDefault(specification, nameof(ChartSpecification.XAxisLineColor), theme.AxisLine);
		SetIfDefault(specification, nameof(ChartSpecification.YAxisLineColor), theme.AxisLine);
		SetIfDefault(specification, nameof(ChartSpecification.XAxisMajorGridColor), theme.MajorGrid);
		SetIfDefault(specification, nameof(ChartSpecification.YAxisMajorGridColor), theme.MajorGrid);
		SetIfDefault(specification, nameof(ChartSpecification.XAxisMinorGridColor), theme.MinorGrid);
		SetIfDefault(specification, nameof(ChartSpecification.YAxisMinorGridColor), theme.MinorGrid);
		SetIfDefault(specification, nameof(ChartSpecification.AxisLabelColor), theme.AxisLabel);
		SetIfDefault(specification, nameof(ChartSpecification.LegendFontColor), theme.AxisLabel);
		SetIfDefault(specification, nameof(ChartSpecification.InnerPlotYPositionPercent), 12);
		SetIfDefault(specification, nameof(ChartSpecification.InnerPlotHeightPercent), 80);
		SetIfDefault(specification, nameof(ChartSpecification.InnerPlotWidthPercent), 86);
		SetIfDefault(specification, nameof(ChartSpecification.XAxisFontSize), 12d);
		SetIfDefault(specification, nameof(ChartSpecification.YAxisFontSize), 12d);
		SetIfDefault(specification, nameof(ChartSpecification.LegendFontSize), 13d);
	}

	/// <summary>
	/// Sets a property only where it still holds the value a fresh specification would.
	/// </summary>
	private static void SetIfDefault(ChartSpecification specification, string name, object value)
	{
		if (SpecificationEditor.IsDefault(specification, name))
		{
			SpecificationEditor.WriteValue(specification, name, value);
		}
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
			"Axis furniture",
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
			"Marker styles",
			"Every marker style the enum declares, one series each. Until recently only Circle "
			+ "was implemented and the rest threw, so a chart asking for squares failed outright.",
			SampleStatus.Working,
			BuildMarkerGallery()),

		new(
			"Explicit range",
			"An axis minimum and maximum given outright, which is honoured exactly rather than "
			+ "adjusted. This is what a dynamic Y axis computes.",
			SampleStatus.Working,
			WithRange(WithAxes(Build(SeriesChartType.Line, markers: true), "Day", "Percent"), 5, 40)),

		new(
			"Gridlines",
			"Major gridlines on both axes and minor gridlines on the value axis, each in its own "
			+ "colour.",
			SampleStatus.Working,
			BuildGridlines()),

		new(
			"Labels at 90 degrees",
			"Category labels rotated a quarter turn, the usual treatment when the labels are "
			+ "longer than the band is wide.",
			SampleStatus.Working,
			WithLabelAngle(WithAxes(BuildLongCategories(), "Region", "Sales"), -90)),

		new(
			"Label format",
			"An explicit format string on the value axis, so the numbers carry a decimal place "
			+ "and a thousands separator.",
			SampleStatus.Working,
			BuildFormatted()),

		new(
			"Legend at the side",
			"The legend as a column on the right, which is what a narrow legend needs once there "
			+ "are more than two or three series.",
			SampleStatus.Working,
			WithLegendColumn(WithAxes(BuildMultiSeries(SeriesChartType.Column), "Day", "Percent"))),

		new(
			"Legend below",
			"The legend as a row beneath the plot, with the chart area shortened to make room. "
			+ "Positions are percentages of the image, measured from the bottom left.",
			SampleStatus.Working,
			WithLegendBelow(WithAxes(BuildMultiSeries(SeriesChartType.Column), "Day", "Percent"))),

		new(
			"Doughnut, 30% hole",
			"The hole radius given explicitly rather than left at the 60% default.",
			SampleStatus.Working,
			WithDoughnutHole(BuildPie(SeriesChartType.Doughnut), 30)),

		new(
			"Negative values",
			"A series crossing zero. The axis extends below it by a whole interval, and the "
			+ "columns are drawn from the zero line in both directions.",
			SampleStatus.Working,
			WithAxes(BuildNegative(), "Month", "Net change")),

		new(
			"A single data point",
			"One point, which is the degenerate case that tends to divide by zero somewhere.",
			SampleStatus.Working,
			WithAxes(BuildSinglePoint(), "Day", "Percent")),

		new(
			"Many categories",
			"Twenty-four categories, enough that the labels crowd. Thinning them out when they "
			+ "no longer fit is not implemented, so they overlap - visible here rather than "
			+ "discovered later.",
			SampleStatus.Partial,
			WithLabelAngle(WithAxes(BuildManyCategories(), "Hour", "Requests"), -45)),

		new(
			"Stacked bar",
			"Horizontal bars stacked within each category.",
			SampleStatus.Working,
			WithAxes(BuildMultiSeries(SeriesChartType.StackedBar), "Percent", "Day")),

		new(
			"Column and line",
			"A column series and a line series in one plot, both following the same category "
			+ "mapping so they stay aligned.",
			SampleStatus.Working,
			BuildMixed()),

		new(
			"Pie, no labels",
			"Slice labels turned off, leaving the legend to name them.",
			SampleStatus.Working,
			WithPieLabels(BuildPie(SeriesChartType.Pie), "Disabled")),

		new(
			"Doughnut, outside labels",
			"A ring with its labels outside on leader lines.",
			SampleStatus.Working,
			WithPieLabels(BuildPie(SeriesChartType.Doughnut), "Outside")),

		new(
			"Point",
			"One of the chart types the enum declares and the renderer has no case for, so it "
			+ "draws nothing. Issue #33 tracks the remainder.",
			SampleStatus.NotImplemented,
			WithAxes(BuildMultiSeries(SeriesChartType.Point), "Day", "Percent")),

		new(
			"100% stacked column",
			"Deliberately still blank. Rendering these needs the value axis rescaled to 0-100 "
			+ "per category, which is not wired up, and a chart showing plausible but wrong "
			+ "proportions would be worse than one showing nothing. Issue #33.",
			SampleStatus.NotImplemented,
			WithAxes(BuildMultiSeries(SeriesChartType.StackedColumn100), "Day", "Percent")),

		new(
			"Pie",
			"One series, one slice per point, coloured per point. Angles run clockwise from "
			+ "three o'clock, as the Microsoft chart control does, and each slice is labelled "
			+ "with its category name.",
			SampleStatus.Working,
			BuildPie(SeriesChartType.Pie)),

		new(
			"Doughnut",
			"The same data as a ring. The hole is 60% of the radius, matching the Microsoft "
			+ "chart control default, and is configurable.",
			SampleStatus.Working,
			BuildPie(SeriesChartType.Doughnut)),

		new(
			"Pie, outside labels",
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

	private static ChartSpecification WithPieLabels(ChartSpecification specification, string style)
	{
		specification.PieLabelStyle = style;
		return specification;
	}

	private static ChartSpecification BuildMixed()
	{
		var specification = new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					LegendText = "Volume",
					StrokeColor = Color.SteelBlue,
					FillColor = Color.SteelBlue,
					StrokeWidth = 1,
					IsXValueIndexed = true,
					Points = Points(12, 19, 14, 22, 26, 21, 30)
				},
				new()
				{
					ChartType = SeriesChartType.Line,
					LegendText = "Target",
					StrokeColor = Color.IndianRed,
					StrokeWidth = 3,
					IsXValueIndexed = true,
					MarkerStyle = MarkerStyle.Circle,
					MarkerFillColor = Color.White,
					MarkerStrokeColor = Color.IndianRed,
					MarkerSize = 6,
					Points = Points(18, 18, 18, 20, 20, 22, 22)
				}
			]
		};

		return WithAxes(specification, "Day", "Percent");
	}

	private static ChartSpecification WithRange(ChartSpecification specification, double minimum, double maximum)
	{
		specification.YAxisMinimum = minimum;
		specification.YAxisMaximum = maximum;
		return specification;
	}

	private static ChartSpecification WithLabelAngle(ChartSpecification specification, int degrees)
	{
		specification.XAxisLabelAngle = degrees;
		return specification;
	}

	private static ChartSpecification WithDoughnutHole(ChartSpecification specification, int percent)
	{
		specification.DoughnutRadius = percent;
		return specification;
	}

	/// <summary>
	/// The legend as a column on the right. Positions are percentages of the image measured from
	/// the bottom left, so a full-height legend on the right is x 78, y 0.
	/// </summary>
	private static ChartSpecification WithLegendColumn(ChartSpecification specification)
	{
		specification.LegendStyle = LegendStyle.Column;
		specification.LegendXPositionPercent = 78;
		specification.LegendYPositionPercent = 0;
		specification.LegendWidthPercent = 22;
		specification.LegendHeightPercent = 100;
		specification.ChartAreaWidthPercent = 78;
		return specification;
	}

	private static ChartSpecification WithLegendBelow(ChartSpecification specification)
	{
		specification.LegendStyle = LegendStyle.Row;
		specification.LegendXPositionPercent = 0;
		specification.LegendYPositionPercent = 0;
		specification.LegendWidthPercent = 100;
		specification.LegendHeightPercent = 16;
		specification.ChartAreaXPositionPercent = 0;
		specification.ChartAreaYPositionPercent = 16;
		specification.ChartAreaWidthPercent = 100;
		specification.ChartAreaHeightPercent = 84;
		return specification;
	}

	private static ChartSpecification BuildMarkerGallery()
	{
		var styles = new[]
		{
			MarkerStyle.Circle,
			MarkerStyle.Square,
			MarkerStyle.Diamond,
			MarkerStyle.Triangle,
			MarkerStyle.Cross,
			MarkerStyle.Star4,
			MarkerStyle.Star5,
			MarkerStyle.Star6
		};
		var colours = new[]
		{
			Color.SteelBlue,
			Color.SeaGreen,
			Color.Goldenrod,
			Color.IndianRed,
			Color.MediumPurple,
			Color.DarkCyan,
			Color.Chocolate,
			Color.SlateGray
		};

		var specification = new ChartSpecification { LegendStyle = LegendStyle.Column };
		WithLegendColumn(specification);

		for (var index = 0; index < styles.Length; index++)
		{
			// One flat series per style, stacked up the plot, so each marker is on its own line
			// and can be told apart.
			var level = 4 + (index * 4);
			specification.SeriesList.Add(new SeriesSpecification
			{
				ChartType = SeriesChartType.Line,
				LegendText = styles[index].ToString(),
				StrokeColor = colours[index],
				StrokeWidth = 2,
				IsXValueIndexed = true,
				MarkerStyle = styles[index],
				MarkerSize = 12,
				MarkerFillColor = Color.White,
				MarkerStrokeColor = colours[index],
				MarkerStrokeWidth = 2,
				Points = Points(level, level, level, level, level)
			});
		}

		return WithAxes(specification, "Point", "Series");
	}

	private static ChartSpecification BuildGridlines()
	{
		var specification = WithAxes(BuildMultiSeries(SeriesChartType.Line), "Day", "Percent");
		specification.XAxisMajorGridEnabled = true;
		specification.YAxisMajorGridEnabled = true;
		specification.YAxisMinorGridEnabled = true;
		specification.XAxisMajorGridColor = Color.FromArgb(0xC0, 0xC8, 0xD0);
		specification.YAxisMajorGridColor = Color.FromArgb(0xC0, 0xC8, 0xD0);
		specification.YAxisMinorGridColor = Color.FromArgb(0xE8, 0xEC, 0xF0);
		specification.LegendStyle = LegendStyle.Column;
		WithLegendColumn(specification);
		return specification;
	}

	private static ChartSpecification BuildFormatted()
	{
		var specification = new ChartSpecification
		{
			YAxisLabelFormat = "#,##0.0",
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					LegendText = "Requests",
					StrokeColor = Color.SteelBlue,
					FillColor = Color.SteelBlue,
					StrokeWidth = 1,
					IsXValueIndexed = true,
					Points = Points(1240, 1890, 1410, 2260, 2610, 2130, 3020)
				}
			]
		};

		return WithAxes(specification, "Day", "Requests");
	}

	private static ChartSpecification BuildLongCategories()
	{
		var regions = new[] { "North West", "North East", "Midlands", "South West", "South East" };
		var values = new double[] { 42, 31, 55, 28, 61 };

		return new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					LegendText = "Sales",
					StrokeColor = Color.SeaGreen,
					FillColor = Color.SeaGreen,
					StrokeWidth = 1,
					IsXValueIndexed = true,
					Points = [.. values.Select((value, i) => new ChartPoint(regions[i], i, value))]
				}
			]
		};
	}

	private static ChartSpecification BuildNegative()
	{
		var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
		var values = new double[] { 18, -7, 12, -14, 4, 21 };

		return new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					LegendText = "Net change",
					StrokeColor = Color.SteelBlue,
					FillColor = Color.SteelBlue,
					StrokeWidth = 1,
					IsXValueIndexed = true,
					Points = [.. values.Select((value, i) => new ChartPoint(months[i], i, value))]
				}
			]
		};
	}

	private static ChartSpecification BuildSinglePoint()
		=> new()
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Column,
					LegendText = "Utilisation",
					StrokeColor = Color.Goldenrod,
					FillColor = Color.Goldenrod,
					StrokeWidth = 1,
					IsXValueIndexed = true,
					Points = [new ChartPoint("Mon", 0, 42)]
				}
			]
		};

	private static ChartSpecification BuildManyCategories()
	{
		var points = new List<ChartPoint>();
		for (var hour = 0; hour < 24; hour++)
		{
			// A plausible daily shape: quiet overnight, busy through the working day.
			var value = 40 + (60 * Math.Sin(Math.Max(0, hour - 5) / 14.0 * Math.PI));
			points.Add(new ChartPoint(
				FormattableString.Invariant($"{hour:00}:00"),
				hour,
				Math.Round(Math.Max(8, value))));
		}

		return new ChartSpecification
		{
			SeriesList =
			[
				new()
				{
					ChartType = SeriesChartType.Line,
					LegendText = "Requests",
					StrokeColor = Color.IndianRed,
					StrokeWidth = 2,
					IsXValueIndexed = true,
					Points = points
				}
			]
		};
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
