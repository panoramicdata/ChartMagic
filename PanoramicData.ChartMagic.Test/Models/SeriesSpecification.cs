using System.Drawing;

namespace PanoramicData.ChartMagic.Test.Models;

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
}
