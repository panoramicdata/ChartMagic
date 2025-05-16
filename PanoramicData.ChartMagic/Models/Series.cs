using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

public class Series(ChartElement parent, string name) : ChartNamedElement(parent, name)
{
	public List<ChartPoint> Points { get; set; } = [];
	public SeriesChartType ChartType { get; set; }
	public bool IsXValueIndexed { get; set; }
	public string? LabelText { get; set; }
	public string? LegendText { get; set; }
	public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.None;
	public Color? MarkerStrokeColor { get; set; }
	public Color? MarkerFillColor { get; set; }
	public double? MarkerStrokeWidth { get; set; }
	public double? MarkerSize { get; set; }
}
