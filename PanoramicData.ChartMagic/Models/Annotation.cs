namespace PanoramicData.ChartMagic.Models;

public class Annotation : ChartNamedElement
{
	public Annotation(IChartElement parent, string name) : base(parent, name)
	{
	}

	public string Text { get; set; } = string.Empty;

	public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;

	public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Middle;
}
