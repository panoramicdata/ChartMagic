using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

/// <summary>
/// A declarative description of a chart: series, areas, axes, legend and annotations.
/// Call <see cref="ToChart"/> to turn it into a renderable <see cref="Chart"/>.
/// </summary>
/// <remarks>
/// The defaults here are deliberately non-zero. Chart elements are laid out relative to
/// their parent, so an element left at zero width or height renders nothing at all.
/// </remarks>
public class ChartSpecification
{
	public List<SeriesSpecification> SeriesList { get; set; } = [];

	public object? DoughnutRadius { get; set; }

	public Color ChartBackgroundColor { get; set; } = Colors.Transparent;
	public Color ChartBorderColor { get; set; } = Colors.Transparent;
	public int ChartBorderWidth { get; set; } = 2;
	public ChartDashStyle ChartBorderLineDashStyle { get; set; }

	public double ChartAreaXPositionPercent { get; set; }
	public double ChartAreaYPositionPercent { get; set; }
	public double ChartAreaXRadiusPixels { get; set; } = 10;
	public double ChartAreaYRadiusPixels { get; set; } = 10;
	public double ChartAreaWidthPercent { get; set; } = 65;
	public double ChartAreaHeightPercent { get; set; } = 100;
	public Color ChartAreaBackgroundColor { get; set; } = Colors.Transparent;
	public Color ChartAreaBorderColor { get; set; } = Colors.Transparent;
	public int ChartAreaBorderWidth { get; set; } = 2;
	public ChartDashStyle ChartAreaBorderLineDashStyle { get; set; }

	public bool EnsureColorsUnique { get; set; }

	public int InnerPlotXPositionPercent { get; set; } = 10;
	public int InnerPlotYPositionPercent { get; set; } = 10;
	public int InnerPlotWidthPercent { get; set; } = 90;
	public int InnerPlotHeightPercent { get; set; } = 90;
	public double InnerPlotXRadiusPixels { get; set; } = 5;
	public double InnerPlotYRadiusPixels { get; set; } = 5;
	public double InnerPlotFontSize { get; set; } = 20;
	public Color InnerPlotBackgroundColor { get; set; } = Colors.Transparent;
	public Color InnerPlotBorderColor { get; set; } = Colors.Transparent;
	public int InnerPlotBorderWidth { get; set; } = 2;
	public ChartDashStyle InnerPlotBorderLineDashStyle { get; set; }

	#region Legend
	public LegendStyle LegendStyle { get; set; } = LegendStyle.Row;
	public double LegendXPositionPercent { get; set; } = 65;
	public double LegendYPositionPercent { get; set; }
	public double LegendWidthPercent { get; set; } = 35;
	public double LegendHeightPercent { get; set; } = 100;
	public double LegendXRadiusPixels { get; set; } = 5;
	public double LegendYRadiusPixels { get; set; } = 5;
	public double LegendFontSize { get; set; } = 20;
	public Color LegendBackgroundColor { get; set; } = Colors.Transparent;
	public Color LegendBorderColor { get; set; } = Colors.Transparent;
	public int LegendBorderWidth { get; set; } = 2;
	public ChartDashStyle LegendBorderLineDashStyle { get; set; }
	#endregion


	public List<string> Labels { get; set; } = [];
	public double? LabelFontSize { get; set; }
	public Color? LabelColor { get; set; }
	public Color LabelBackgroundColor { get; set; } = Colors.Transparent;

	public List<string> Palette { get; set; } = [];
	public List<AnnotationSpec> AnnotationList { get; set; } = [];
	public string? PieLabelStyle { get; set; }
	public Color PieLineColor { get; set; } = Color.Black;
	public int PieStartAngleDegrees { get; set; }
	public int PieSweepAngleDegrees { get; set; }
	public Color PieCollectedColor { get; set; } = Color.Gray;
	public string? PieCollectedLabel { get; set; }
	public double PieCollectedThresholdPercent { get; set; }

	public ChartValueType XValueType { get; set; }

	public Color XAxisBackgroundColor { get; set; } = Colors.Transparent;
	public IntervalAutoMode XAxisIntervalAutoMode { get; set; }
	public DateTimeIntervalType XAxisIntervalType { get; set; }
	public double? XAxisInterval { get; set; }
	public bool XAxisIsAutoFit { get; set; }
	public int XAxisLabelAngle { get; set; }
	public LabelAutoFitStyles XAxisLabelAutoFitStyle { get; set; }
	public string? XAxisTitle { get; set; }
	public bool XAxisMajorGridEnabled { get; set; }
	public DateTimeIntervalType? XAxisMajorGridIntervalType { get; set; }
	public double? XAxisMajorGridInterval { get; set; }
	public bool XAxisMinorGridEnabled { get; set; }
	public DateTimeIntervalType XAxisMinorGridIntervalType { get; set; }
	public double? XAxisMinorGridInterval { get; set; }
	public double XAxisFontSize { get; set; } = 20;
	public string? XAxisLabelFormat { get; set; }
	public bool XAxisIsLogarithmic { get; set; }

	public Color YAxisBackgroundColor { get; set; } = Colors.Transparent;
	public double? YAxisMinimum { get; set; }
	public double? YAxisMaximum { get; set; }
	public double? YAxisInterval { get; set; }
	public bool YAxisIsAutoFit { get; set; }
	public double? YAxisWidthPercent { get; set; } = null;
	public bool YAxisMajorGridEnabled { get; set; }
	public DateTimeIntervalType YAxisMajorGridIntervalType { get; set; }
	public double? YAxisMajorGridInterval { get; set; }
	public bool YAxisMinorGridEnabled { get; set; }
	public DateTimeIntervalType YAxisMinorGridIntervalType { get; set; }
	public double? YAxisMinorGridInterval { get; set; }
	public double YAxisFontSize { get; set; } = 20;
	public string? YAxisTitle { get; set; }
	public bool UseYAxisShortLabels { get; set; }
	public IntervalAutoMode YAxisIntervalAutoMode { get; set; }
	public DateTimeIntervalType YAxisIntervalType { get; set; }
	public int YAxisLabelAngle { get; set; }
	public LabelAutoFitStyles YAxisLabelAutoFitStyle { get; set; }
	public string? YAxisLabelFormat { get; set; }
	public bool YAxisIsLogarithmic { get; set; }

	/// <summary>Colour of the X axis line and its tick marks.</summary>
	public Color XAxisLineColor { get; set; } = Color.FromArgb(0x59, 0x59, 0x59);

	/// <summary>Colour of the Y axis line and its tick marks.</summary>
	public Color YAxisLineColor { get; set; } = Color.FromArgb(0x59, 0x59, 0x59);

	/// <summary>Colour of X axis major gridlines.</summary>
	public Color XAxisMajorGridColor { get; set; } = Color.FromArgb(0xD9, 0xD9, 0xD9);

	/// <summary>Colour of Y axis major gridlines.</summary>
	public Color YAxisMajorGridColor { get; set; } = Color.FromArgb(0xD9, 0xD9, 0xD9);

	/// <summary>Colour of X axis minor gridlines.</summary>
	public Color XAxisMinorGridColor { get; set; } = Color.FromArgb(0xED, 0xED, 0xED);

	/// <summary>Colour of Y axis minor gridlines.</summary>
	public Color YAxisMinorGridColor { get; set; } = Color.FromArgb(0xED, 0xED, 0xED);

	/// <summary>Colour of axis tick labels and titles.</summary>
	public Color AxisLabelColor { get; set; } = Color.FromArgb(0x33, 0x33, 0x33);

	/// <summary>Colour of legend label text.</summary>
	public Color LegendFontColor { get; set; } = Color.FromArgb(0x33, 0x33, 0x33);

	/// <summary>Fraction of a category band a group of columns occupies, 0 to 1.</summary>
	public double ColumnBandFillFraction { get; set; } = 0.8;

	public bool Enable3d { get; set; }
	public int Inclination3dDegrees { get; set; }
	public int Rotation3dDegrees { get; set; }
	public int Perspective3dPercent { get; set; }
	public int PointDepth3dPercent { get; set; }
	public int PointGapDepth3dPercent { get; set; }

	/// <summary>
	/// Builds a <see cref="Chart"/> from this specification, wired up and sized ready to
	/// render.
	/// </summary>
	/// <remarks>
	/// Issue #29: this was previously internal to the test project, so consumers had to
	/// assemble the object model by hand with no reference to follow. Two of the assignments
	/// below are load-bearing and impossible to guess - the root background area must be
	/// sized, and each series must take its height from the inner plot. Omitting either
	/// yields a blank chart with no error, which is exactly what happened downstream.
	/// </remarks>
	/// <summary>
	/// The doughnut hole radius as a number. It is declared as an object because the
	/// corresponding Microsoft chart custom property is a string, and callers pass whichever
	/// of the two they happen to hold.
	/// </summary>
	private double? DoughnutRadiusAsPercent()
	{
		return DoughnutRadius switch
		{
			null => null,
			double d => d,
			int i => i,
			string text when double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
			_ => null
		};
	}

	/// <summary>
	/// The pie label style, which arrives as one of the Microsoft chart control strings.
	/// </summary>
	private PieLabelStyle ParsePieLabelStyle()
		=> Enum.TryParse<PieLabelStyle>(PieLabelStyle, ignoreCase: true, out var style)
			? style
			: Models.PieLabelStyle.Inside;

	public Chart ToChart()
	{
		var chart = new Chart();

		// ChartBackgroundArea
		var chartBackgroundArea = new ChartBackgroundArea(chart, "Chart Background")
		{
			FillColor = ChartBackgroundColor,
			WidthPercent = 100,
			HeightPercent = 100,
			StrokeColor = ChartBorderColor,
			StrokeWidth = ChartBorderWidth,
			StrokeStyle = ChartBorderLineDashStyle,
			FontSize = 20
		};
		chart.ChartBackgroundArea = chartBackgroundArea;

		// Legend
		var legend = new Legend(chart, "Legend")
		{
			XPositionPercent = LegendXPositionPercent,
			YPositionPercent = LegendYPositionPercent,
			XRadiusPixels = LegendXRadiusPixels,
			YRadiusPixels = LegendYRadiusPixels,
			WidthPercent = LegendWidthPercent,
			HeightPercent = LegendHeightPercent,
			FillColor = LegendBackgroundColor,
			StrokeColor = LegendBorderColor,
			StrokeWidth = LegendBorderWidth,
			StrokeStyle = LegendBorderLineDashStyle,
			FontSize = LegendFontSize,
			Style = LegendStyle,
			FontColor = LegendFontColor,
		};
		chart.Legends.Add(legend);

		// ChartArea
		chart.ChartArea.XPositionPercent = ChartAreaXPositionPercent;
		chart.ChartArea.YPositionPercent = ChartAreaYPositionPercent;
		chart.ChartArea.XRadiusPixels = ChartAreaXRadiusPixels;
		chart.ChartArea.YRadiusPixels = ChartAreaYRadiusPixels;
		chart.ChartArea.WidthPercent = ChartAreaWidthPercent;
		chart.ChartArea.HeightPercent = ChartAreaHeightPercent;
		chart.ChartArea.FillColor = ChartAreaBackgroundColor;
		chart.ChartArea.StrokeColor = ChartAreaBorderColor;
		chart.ChartArea.StrokeWidth = ChartAreaBorderWidth;
		chart.ChartArea.StrokeStyle = ChartAreaBorderLineDashStyle;

		// ChartArea.InnerPlot
		chart.ChartArea.InnerPlot.XPositionPercent = InnerPlotXPositionPercent;
		chart.ChartArea.InnerPlot.YPositionPercent = InnerPlotYPositionPercent;
		chart.ChartArea.InnerPlot.WidthPercent = InnerPlotWidthPercent;
		chart.ChartArea.InnerPlot.HeightPercent = InnerPlotHeightPercent;
		chart.ChartArea.InnerPlot.XRadiusPixels = InnerPlotXRadiusPixels;
		chart.ChartArea.InnerPlot.YRadiusPixels = InnerPlotYRadiusPixels;
		chart.ChartArea.InnerPlot.FontSize = InnerPlotFontSize;
		chart.ChartArea.InnerPlot.FillColor = InnerPlotBackgroundColor;
		chart.ChartArea.InnerPlot.StrokeColor = InnerPlotBorderColor;
		chart.ChartArea.InnerPlot.StrokeWidth = InnerPlotBorderWidth;
		chart.ChartArea.InnerPlot.StrokeStyle = InnerPlotBorderLineDashStyle;
		chart.ChartArea.ColumnBandFillFraction = ColumnBandFillFraction;

		// XAxis
		//
		// Issue #31: the geometry below was wired up, but none of the settings that decide what
		// the axis actually says were - not the title, the label angle, the gridlines, the
		// interval, the label format nor the logarithmic flag. A specification could set every
		// one of them and reach a renderer that had never been told. Fixing the renderer alone
		// would still have drawn nothing.
		chart.ChartArea.XAxis.XPositionPercent = InnerPlotXPositionPercent;
		chart.ChartArea.XAxis.YPositionPercent = 0;
		chart.ChartArea.XAxis.WidthPercent = InnerPlotWidthPercent;
		chart.ChartArea.XAxis.HeightPercent = InnerPlotYPositionPercent;
		chart.ChartArea.XAxis.FillColor = XAxisBackgroundColor;
		chart.ChartArea.XAxis.FontSize = XAxisFontSize;
		chart.ChartArea.XAxis.FontColor = AxisLabelColor;
		chart.ChartArea.XAxis.Title = XAxisTitle;
		chart.ChartArea.XAxis.LabelAngle = XAxisLabelAngle;
		chart.ChartArea.XAxis.LabelFormat = XAxisLabelFormat;
		chart.ChartArea.XAxis.LabelAutoFitStyle = XAxisLabelAutoFitStyle;
		chart.ChartArea.XAxis.IsAutoFit = XAxisIsAutoFit;
		chart.ChartArea.XAxis.Interval = XAxisInterval;
		chart.ChartArea.XAxis.IntervalType = XAxisIntervalType;
		chart.ChartArea.XAxis.XAxisIntervalAutoMode = XAxisIntervalAutoMode;
		chart.ChartArea.XAxis.IsLogarithmic = XAxisIsLogarithmic;
		chart.ChartArea.XAxis.MajorGridEnabled = XAxisMajorGridEnabled;
		chart.ChartArea.XAxis.MajorGridInterval = XAxisMajorGridInterval;
		chart.ChartArea.XAxis.MajorGridIntervalType = XAxisMajorGridIntervalType;
		chart.ChartArea.XAxis.MinorGridEnabled = XAxisMinorGridEnabled;
		chart.ChartArea.XAxis.MinorGridInterval = XAxisMinorGridInterval;
		chart.ChartArea.XAxis.MinorGridIntervalType = XAxisMinorGridIntervalType;
		chart.ChartArea.XAxis.LineColor = XAxisLineColor;
		chart.ChartArea.XAxis.MajorGridColor = XAxisMajorGridColor;
		chart.ChartArea.XAxis.MinorGridColor = XAxisMinorGridColor;

		// YAxis
		chart.ChartArea.YAxis.XPositionPercent = 0;
		chart.ChartArea.YAxis.YPositionPercent = InnerPlotYPositionPercent;
		chart.ChartArea.YAxis.WidthPercent = YAxisWidthPercent ?? InnerPlotXPositionPercent;
		chart.ChartArea.YAxis.HeightPercent = InnerPlotHeightPercent;
		chart.ChartArea.YAxis.FillColor = YAxisBackgroundColor;
		chart.ChartArea.YAxis.FontSize = YAxisFontSize;
		chart.ChartArea.YAxis.FontColor = AxisLabelColor;
		chart.ChartArea.YAxis.Title = YAxisTitle;
		chart.ChartArea.YAxis.LabelAngle = YAxisLabelAngle;
		chart.ChartArea.YAxis.LabelFormat = YAxisLabelFormat;
		chart.ChartArea.YAxis.LabelAutoFitStyle = YAxisLabelAutoFitStyle;
		chart.ChartArea.YAxis.IsAutoFit = YAxisIsAutoFit;
		chart.ChartArea.YAxis.Min = YAxisMinimum;
		chart.ChartArea.YAxis.Max = YAxisMaximum;
		chart.ChartArea.YAxis.Interval = YAxisInterval;
		chart.ChartArea.YAxis.IntervalType = YAxisIntervalType;
		chart.ChartArea.YAxis.XAxisIntervalAutoMode = YAxisIntervalAutoMode;
		chart.ChartArea.YAxis.IsLogarithmic = YAxisIsLogarithmic;
		chart.ChartArea.YAxis.MajorGridEnabled = YAxisMajorGridEnabled;
		chart.ChartArea.YAxis.MajorGridInterval = YAxisMajorGridInterval;
		chart.ChartArea.YAxis.MajorGridIntervalType = YAxisMajorGridIntervalType;
		chart.ChartArea.YAxis.MinorGridEnabled = YAxisMinorGridEnabled;
		chart.ChartArea.YAxis.MinorGridInterval = YAxisMinorGridInterval;
		chart.ChartArea.YAxis.MinorGridIntervalType = YAxisMinorGridIntervalType;
		chart.ChartArea.YAxis.LineColor = YAxisLineColor;
		chart.ChartArea.YAxis.MajorGridColor = YAxisMajorGridColor;
		chart.ChartArea.YAxis.MinorGridColor = YAxisMinorGridColor;
		chart.ChartArea.YAxis.UseShortLabels = UseYAxisShortLabels;

		// Series
		var seriesIndex = 0;
		foreach (var seriesSpec in SeriesList)
		{
			var series = new Series(chart.ChartArea, $"Series {++seriesIndex}")
			{
				ChartType = seriesSpec.ChartType,
				FillColor = seriesSpec.FillColor,
				FontSize = seriesSpec.FontSize,
				HeightPercent = chart.ChartArea.InnerPlot.HeightPercent,
				IsXValueIndexed = seriesSpec.IsXValueIndexed,
				LabelText = seriesSpec.LabelText,
				LegendText = seriesSpec.LegendText,

				MarkerStyle = seriesSpec.MarkerStyle,
				MarkerStrokeColor = seriesSpec.MarkerStrokeColor,
				MarkerFillColor = seriesSpec.MarkerFillColor,
				MarkerStrokeWidth = seriesSpec.MarkerStrokeWidth,
				MarkerSize = seriesSpec.MarkerSize,

				Points = seriesSpec.Points,

				StrokeColor = seriesSpec.StrokeColor,
				StrokeLineCapStyle = seriesSpec.StrokeLineCapStyle,
				StrokeLineJoinStyle = seriesSpec.StrokeLineJoinStyle,
				StrokeStyle = seriesSpec.StrokeStyle,
				StrokeWidth = seriesSpec.StrokeWidth,

			// Pie settings come from the chart unless the series overrides them: the Microsoft
			// chart control holds them per series, but a specification sets them once.
			DoughnutRadiusPercent = seriesSpec.DoughnutRadiusPercent ?? DoughnutRadiusAsPercent(),
			PieLabelStyle = seriesSpec.PieLabelStyle != default ? seriesSpec.PieLabelStyle : ParsePieLabelStyle(),
			PieLineColor = seriesSpec.PieLineColor ?? PieLineColor,
			PieStartAngleDegrees = seriesSpec.PieStartAngleDegrees ?? PieStartAngleDegrees,
			PieCollectedThresholdPercent = seriesSpec.PieCollectedThresholdPercent ?? PieCollectedThresholdPercent,
			PieCollectedColor = seriesSpec.PieCollectedColor ?? PieCollectedColor,
			PieCollectedLabel = seriesSpec.PieCollectedLabel ?? PieCollectedLabel,
			};
			chart.Series.Add(series);
		}

		// Annotations
		var annotationIndex = 0;
		foreach (var annotationSpec in AnnotationList)
		{
			var annotation = new Annotation(chartBackgroundArea, $"Annotation {++annotationIndex}")
			{
				// Group
				StrokeColor = annotationSpec.StrokeColor,
				StrokeWidth = annotationSpec.StrokeWidth,
				StrokeStyle = annotationSpec.StrokeStyle,
				XPositionPercent = annotationSpec.XPositionPercent,
				YPositionPercent = annotationSpec.YPositionPercent,
				XRadiusPixels = annotationSpec.XRadiusPixels,
				YRadiusPixels = annotationSpec.YRadiusPixels,
				FillColor = annotationSpec.FillColor,
				FontSize = annotationSpec.FontSize,
				FontWeight = annotationSpec.FontWeight,
				FontFamily = annotationSpec.FontFamily,

				// Annotation-specific
				Text = annotationSpec.Text,
				HorizontalAlignment = annotationSpec.HorizontalAlignment,
				VerticalAlignment = annotationSpec.VerticalAlignment,
			};
			chart.Annotations.Add(annotation);
		}

		return chart;
	}
}
