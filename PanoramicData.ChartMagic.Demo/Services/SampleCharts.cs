using PanoramicData.ChartMagic.Models;
using System.Drawing;
using System.Text;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// The sample gallery, and the SVG rendering used to display it.
/// </summary>
public static partial class SampleCharts
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
			"Each category scaled to fill the plot, so the segments read as shares rather than "
			+ "amounts. The value axis is a fixed nought to a hundred whatever the totals are.",
			SampleStatus.Working,
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
}
