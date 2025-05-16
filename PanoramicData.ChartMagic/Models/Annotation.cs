namespace PanoramicData.ChartMagic.Models;

public class Annotation(IChartElement parent, string name) : ChartNamedElement(parent, name)
{
	public string Text { get; set; } = string.Empty;

	public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;

	public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Middle;
}
