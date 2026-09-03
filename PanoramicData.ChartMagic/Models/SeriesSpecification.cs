using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

public class SeriesSpecification
{
	public List<ChartPoint> Points { get; set; } = [];
	public SeriesChartType ChartType { get; set; }
	public bool IsXValueIndexed { get; set; }
	public Color FillColor { get; set; } = Colors.Transparent;
	public Color StrokeColor { get; set; }
	public double StrokeWidth { get; set; } = 2;
	public StrokeLineCapStyle StrokeLineCapStyle { get; set; } = StrokeLineCapStyle.Round;
	public StrokeLineJoinStyle StrokeLineJoinStyle { get; set; } = StrokeLineJoinStyle.Round;
	public string? LabelText { get; set; }
	public string? LegendText { get; set; }
	public double FontSize { get; set; } = 20;
	public ChartDashStyle StrokeStyle { get; set; } = ChartDashStyle.Solid;
	public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.None;
	public Color? MarkerStrokeColor { get; set; }
	public Color? MarkerFillColor { get; set; }
	public double? MarkerStrokeWidth { get; set; }
	public double? MarkerSize { get; set; }
	/// <summary>
	/// Overrides <see cref="Series.DoughnutRadiusPercent"/> for this series. Null takes the
	/// chart-wide <see cref="ChartSpecification.DoughnutRadius"/> instead.
	/// </summary>
	public double? DoughnutRadiusPercent { get; set; }
	public PieLabelStyle PieLabelStyle { get; set; }
	public Color? PieLineColor { get; set; }
	public double? PieStartAngleDegrees { get; set; }
	public double? PieCollectedThresholdPercent { get; set; }
	public Color? PieCollectedColor { get; set; }
	public string? PieCollectedLabel { get; set; }
}
