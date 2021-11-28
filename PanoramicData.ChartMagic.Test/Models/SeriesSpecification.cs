namespace PanoramicData.ChartMagic.Test.Models;

public class SeriesSpecification
{
	public List<ChartPoint> Points { get; set; } = new();
	public SeriesChartType ChartType { get; set; }
	public bool IsXValueIndexed { get; set; }
	public ChartValueType XValueType { get; set; }
	public Color FillColor { get; set; } = Colors.Transparent;
	public Color StrokeColor { get; set; }
	public int StrokeWidth { get; set; } = 2;
	public string? LabelText { get; set; }
	public string? LegendText { get; set; }
	public float FontSize { get; set; } = 20;
	public ChartDashStyle StrokeStyle { get; set; } = ChartDashStyle.Solid;
}
