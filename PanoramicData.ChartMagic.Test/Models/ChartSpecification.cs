namespace PanoramicData.ChartMagic.Test.Models;

public class ChartSpecification
{
	public List<SeriesSpecification> SeriesList { get; set; } = new();

	public object? DoughnutRadius { get; set; }

	public int ChartWidth { get; set; } = 1440;
	public int ChartHeight { get; set; } = 1080;
	public Color ChartBackgroundColor { get; set; } = Colors.Transparent;
	public Color ChartBorderColor { get; set; } = Colors.Transparent;
	public int ChartBorderWidth { get; set; } = 2;
	public ChartDashStyle ChartBorderLineDashStyle { get; set; }

	public double ChartAreaXPositionPercent { get; set; } = 0;
	public double ChartAreaYPositionPercent { get; set; } = 0;
	public double ChartAreaXRadius { get; set; } = 0;
	public double ChartAreaYRadius { get; set; } = 0;
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
	public double InnerPlotXRadius { get; set; } = 5;
	public double InnerPlotYRadius { get; set; } = 5;
	public double InnerPlotFontSize { get; set; } = 20;
	public Color InnerPlotBackgroundColor { get; set; } = Colors.Transparent;
	public Color InnerPlotBorderColor { get; set; } = Colors.Transparent;
	public int InnerPlotBorderWidth { get; set; } = 2;
	public ChartDashStyle InnerPlotBorderLineDashStyle { get; set; }

	#region Legend
	public LegendStyle LegendStyle { get; set; }
	public double LegendXPositionPercent { get; set; } = 65;
	public double LegendYPositionPercent { get; set; } = 0;
	public double LegendWidthPercent { get; set; } = 35;
	public double LegendHeightPercent { get; set; } = 100;
	public double LegendXRadius { get; set; } = 5;
	public double LegendYRadius { get; set; } = 5;
	public double LegendFontSize { get; set; } = 20;
	public Color LegendBackgroundColor { get; set; } = Colors.Transparent;
	public Color LegendBorderColor { get; set; } = Colors.Transparent;
	public int LegendBorderWidth { get; set; } = 2;
	public ChartDashStyle LegendBorderLineDashStyle { get; set; }
	#endregion


	public List<string> Labels { get; set; } = new();
	public double? LabelFontSize { get; set; }
	public Color? LabelColor { get; set; }
	public Color LabelBackgroundColor { get; set; } = Colors.Transparent;

	public List<string> Palette { get; set; } = new();
	public List<AnnotationSpec> AnnotationList { get; set; } = new();
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
	public string YAxisTitle { get; set; } = "Y Axis Title";
	public bool UseYAxisShortLabels { get; set; }
	public IntervalAutoMode YAxisIntervalAutoMode { get; set; }
	public DateTimeIntervalType YAxisIntervalType { get; set; }
	public int YAxisLabelAngle { get; set; }
	public LabelAutoFitStyles YAxisLabelAutoFitStyle { get; set; }
	public string? YAxisLabelFormat { get; set; }
	public bool YAxisIsLogarithmic { get; set; }

	public bool Enable3d { get; set; }
	public int Inclination3dDegrees { get; set; }
	public int Rotation3dDegrees { get; set; }
	public int Perspective3dPercent { get; set; }
	public int PointDepth3dPercent { get; set; }
	public int PointGapDepth3dPercent { get; set; }

	internal Chart ToChart()
	{
		var chart = new Chart();

		// ChartBackgroundArea
		var chartBackgroundArea = new ChartBackgroundArea(chart, "Chart Background")
		{
			FillColor = ChartBackgroundColor,
			Width = ChartWidth,
			Height = ChartHeight,
			StrokeColor = ChartBorderColor,
			StrokeWidth = ChartBorderWidth,
			StrokeStyle = ChartBorderLineDashStyle,
			FontSize = 20
		};
		chart.ChartBackgroundArea = chartBackgroundArea;

		// Legend
		var legend = new Legend(chart, "Legend")
		{
			XPositionPercent = LegendXPositionPercent * ChartWidth / 100,
			YPositionPercent = LegendYPositionPercent * ChartHeight / 100,
			XRadiusPixels = LegendXRadius,
			YRadiusPixels = LegendYRadius,
			Width = ChartWidth * LegendWidthPercent / 100,
			Height = ChartHeight * LegendHeightPercent / 100,
			FillColor = LegendBackgroundColor,
			StrokeColor = LegendBorderColor,
			StrokeWidth = LegendBorderWidth,
			StrokeStyle = LegendBorderLineDashStyle,
			FontSize = LegendFontSize,
			Style = LegendStyle,
		};
		chart.Legends.Add(legend);

		// ChartArea
		chart.ChartArea.XPositionPercent = ChartAreaXPositionPercent * ChartWidth / 100;
		chart.ChartArea.YPositionPercent = ChartAreaYPositionPercent * ChartHeight / 100;
		chart.ChartArea.XRadiusPixels = ChartAreaXRadius;
		chart.ChartArea.YRadiusPixels = ChartAreaYRadius;
		chart.ChartArea.Width = ChartWidth * ChartAreaWidthPercent / 100;
		chart.ChartArea.Height = ChartHeight * ChartAreaHeightPercent / 100;
		chart.ChartArea.FillColor = ChartAreaBackgroundColor;
		chart.ChartArea.StrokeColor = ChartAreaBorderColor;
		chart.ChartArea.StrokeWidth = ChartAreaBorderWidth;
		chart.ChartArea.StrokeStyle = ChartAreaBorderLineDashStyle;

		// ChartArea.InnerPlot
		chart.ChartArea.InnerPlot.XPositionPercent = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
		chart.ChartArea.InnerPlot.YPositionPercent = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;
		chart.ChartArea.InnerPlot.Width = InnerPlotWidthPercent * chart.ChartArea.Width / 100;
		chart.ChartArea.InnerPlot.Height = InnerPlotHeightPercent * chart.ChartArea.Height / 100;
		chart.ChartArea.InnerPlot.XRadiusPixels = InnerPlotXRadius;
		chart.ChartArea.InnerPlot.YRadiusPixels = InnerPlotYRadius;
		chart.ChartArea.InnerPlot.FontSize = InnerPlotFontSize;
		chart.ChartArea.InnerPlot.FillColor = InnerPlotBackgroundColor;
		chart.ChartArea.InnerPlot.StrokeColor = InnerPlotBorderColor;
		chart.ChartArea.InnerPlot.StrokeWidth = InnerPlotBorderWidth;
		chart.ChartArea.InnerPlot.StrokeStyle = InnerPlotBorderLineDashStyle;

		// XAxis
		chart.ChartArea.XAxis.FillColor = XAxisBackgroundColor;
		chart.ChartArea.XAxis.XPositionPercent = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
		chart.ChartArea.XAxis.YPositionPercent = 0;
		chart.ChartArea.XAxis.Width = InnerPlotWidthPercent * chart.ChartArea.Width / 100;
		chart.ChartArea.XAxis.Height = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;

		// YAxis
		chart.ChartArea.YAxis.FillColor = YAxisBackgroundColor;
		chart.ChartArea.YAxis.XPositionPercent = 0;
		chart.ChartArea.YAxis.YPositionPercent = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;
		chart.ChartArea.YAxis.Width = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
		chart.ChartArea.YAxis.Height = InnerPlotHeightPercent * chart.ChartArea.Height / 100;

		// Series
		var seriesIndex = 0;
		foreach (var seriesSpec in SeriesList)
		{
			var series = new Series(chart.ChartArea, $"Series {++seriesIndex}")
			{
				ChartType = seriesSpec.ChartType,
				FillColor = seriesSpec.FillColor,
				FontSize = seriesSpec.FontSize,
				Height = chart.ChartArea.InnerPlot.Height,
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

				Width = chart.ChartArea.InnerPlot.Width,
				XPositionPercent = chart.ChartArea.InnerPlot.XPositionPercent,
				XRadiusPixels = chart.ChartArea.InnerPlot.XRadiusPixels,
				YPositionPercent = chart.ChartArea.InnerPlot.YPositionPercent,
				YRadiusPixels = chart.ChartArea.InnerPlot.YRadiusPixels,
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
				Height = annotationSpec.HeightPercent,
				Width = annotationSpec.WidthPercent,

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
