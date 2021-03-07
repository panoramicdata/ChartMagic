using PanoramicData.ChartMagic.Extensions;
using PanoramicData.ChartMagic.Models;
using System.Collections.Generic;
using System.Drawing;

namespace PanoramicData.ChartMagic.Test.Models
{
	public class ChartSpecification
	{
		public List<SeriesSpecification> SeriesList { get; set; } = new();

		public object? DoughnutRadius { get; set; }

		public int ChartWidth { get; set; } = 2560;
		public int ChartHeight { get; set; } = 800;
		public Color ChartBackgroundColor { get; set; } = Colors.Transparent;
		public Color ChartBorderColor { get; set; } = Colors.Transparent;
		public int ChartBorderWidth { get; set; } = 2;
		public ChartDashStyle ChartBorderLineDashStyle { get; set; }

		public float ChartAreaXPositionPercent { get; set; } = 0;
		public float ChartAreaYPositionPercent { get; set; } = 0;
		public float ChartAreaXRadius { get; set; } = 0;
		public float ChartAreaYRadius { get; set; } = 0;
		public float ChartAreaWidthPercent { get; set; } = 80;
		public float ChartAreaHeightPercent { get; set; } = 100;
		public Color ChartAreaBackgroundColor { get; set; } = Colors.Transparent;
		public Color ChartAreaBorderColor { get; set; } = Colors.Transparent;
		public int ChartAreaBorderWidth { get; set; } = 2;
		public ChartDashStyle ChartAreaBorderLineDashStyle { get; set; }

		public bool EnsureColorsUnique { get; set; }

		public List<FixedLine> FixedLines { get; set; } = new();

		public int InnerPlotXPositionPercent { get; set; } = 10;
		public int InnerPlotYPositionPercent { get; set; } = 10;
		public int InnerPlotWidthPercent { get; set; } = 90;
		public int InnerPlotHeightPercent { get; set; } = 90;
		public float InnerPlotXRadius { get; set; } = 5;
		public float InnerPlotYRadius { get; set; } = 5;
		public float InnerPlotFontSize { get; set; } = 20;
		public Color InnerPlotBackgroundColor { get; set; } = Colors.Transparent;
		public Color InnerPlotBorderColor { get; set; } = Colors.Transparent;
		public int InnerPlotBorderWidth { get; set; } = 2;
		public ChartDashStyle InnerPlotBorderLineDashStyle { get; set; }

		#region Legend
		public LegendStyle LegendStyle { get; set; }
		public float LegendXPositionPercent { get; set; } = 80;
		public float LegendYPositionPercent { get; set; } = 00;
		public float LegendWidthPercent { get; set; } = 20;
		public float LegendHeightPercent { get; set; } = 100;
		public float LegendXRadius { get; set; } = 5;
		public float LegendYRadius { get; set; } = 5;
		public float LegendFontSize { get; set; } = 20;
		public Color LegendBackgroundColor { get; set; } = Colors.Transparent;
		public Color LegendBorderColor { get; set; } = Colors.Transparent;
		public int LegendBorderWidth { get; set; } = 2;
		public ChartDashStyle LegendBorderLineDashStyle { get; set; }
		#endregion


		public List<string> Labels { get; set; } = new();
		public double? LabelFontSize { get; set; }
		public Color? LabelColor { get; set; }
		public Color LabelBackgroundColor { get; set; } = Colors.Transparent;

		public List<string>? Palette { get; set; }
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
				XPosition = LegendXPositionPercent * ChartWidth / 100,
				YPosition = LegendYPositionPercent * ChartHeight / 100,
				XRadius = LegendXRadius,
				YRadius = LegendYRadius,
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
			chart.ChartArea.XPosition = ChartAreaXPositionPercent * ChartWidth / 100;
			chart.ChartArea.YPosition = ChartAreaYPositionPercent * ChartHeight / 100;
			chart.ChartArea.XRadius = ChartAreaXRadius;
			chart.ChartArea.YRadius = ChartAreaYRadius;
			chart.ChartArea.Width = ChartWidth * ChartAreaWidthPercent / 100;
			chart.ChartArea.Height = ChartHeight * ChartAreaHeightPercent / 100;
			chart.ChartArea.FillColor = ChartAreaBackgroundColor;
			chart.ChartArea.StrokeColor = ChartAreaBorderColor;
			chart.ChartArea.StrokeWidth = ChartAreaBorderWidth;
			chart.ChartArea.StrokeStyle = ChartAreaBorderLineDashStyle;

			// ChartArea.InnerPlot
			chart.ChartArea.InnerPlot.XPosition = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
			chart.ChartArea.InnerPlot.YPosition = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;
			chart.ChartArea.InnerPlot.Width = InnerPlotWidthPercent * chart.ChartArea.Width / 100;
			chart.ChartArea.InnerPlot.Height = InnerPlotHeightPercent * chart.ChartArea.Height / 100;
			chart.ChartArea.InnerPlot.XRadius = InnerPlotXRadius;
			chart.ChartArea.InnerPlot.YRadius = InnerPlotYRadius;
			chart.ChartArea.InnerPlot.FontSize = InnerPlotFontSize;
			chart.ChartArea.InnerPlot.FillColor = InnerPlotBackgroundColor;
			chart.ChartArea.InnerPlot.StrokeColor = InnerPlotBorderColor;
			chart.ChartArea.InnerPlot.StrokeWidth = InnerPlotBorderWidth;
			chart.ChartArea.InnerPlot.StrokeStyle = InnerPlotBorderLineDashStyle;

			// XAxis
			chart.ChartArea.XAxis.FillColor = XAxisBackgroundColor;
			chart.ChartArea.XAxis.XPosition = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
			chart.ChartArea.XAxis.YPosition = 0;
			chart.ChartArea.XAxis.Width = InnerPlotWidthPercent * chart.ChartArea.Width / 100;
			chart.ChartArea.XAxis.Height = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;

			// YAxis
			chart.ChartArea.YAxis.FillColor = YAxisBackgroundColor;
			chart.ChartArea.YAxis.XPosition = 0;
			chart.ChartArea.YAxis.YPosition = InnerPlotYPositionPercent * chart.ChartArea.Height / 100;
			chart.ChartArea.YAxis.Width = InnerPlotXPositionPercent * chart.ChartArea.Width / 100;
			chart.ChartArea.YAxis.Height = InnerPlotHeightPercent * chart.ChartArea.Height / 100;

			return chart;
		}
	}
}
