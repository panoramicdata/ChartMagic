namespace PanoramicData.ChartMagic.Test.Models;

public class AnnotationSpec
{
	public string Text { get; set; } = string.Empty;
	public HorizontalAlignment HorizontalAlignment { get; set; }
	public VerticalAlignment VerticalAlignment { get; set; }
	public float XPosition { get; set; }
	public float YPosition { get; set; }
	public float XRadius { get; set; }
	public float YRadius { get; set; }
	public float Width { get; set; }
	public float Height { get; set; }
	public Color FillColor { get; set; } = Colors.Transparent;
	public Color StrokeColor { get; set; } = Colors.Transparent;
	public float StrokeWidth { get; set; } = 2;
	public float FontSize { get; set; } = 20;
	public ChartDashStyle StrokeStyle { get; set; }
	public Color FontColor { get; set; }
}
