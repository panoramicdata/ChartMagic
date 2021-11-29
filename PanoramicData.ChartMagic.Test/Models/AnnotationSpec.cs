namespace PanoramicData.ChartMagic.Test.Models;

public class AnnotationSpec
{
	public string Text { get; set; } = string.Empty;
	public HorizontalAlignment HorizontalAlignment { get; set; }
	public VerticalAlignment VerticalAlignment { get; set; }
	public double XPositionPercent { get; set; }
	public double YPositionPercent { get; set; }
	public double XRadiusPixels { get; set; }
	public double YRadiusPixels { get; set; }
	public double WidthPercent { get; set; }
	public double HeightPercent { get; set; }
	public Color FillColor { get; set; } = Colors.Transparent;
	public Color StrokeColor { get; set; } = Colors.Transparent;
	public double StrokeWidth { get; set; } = 2;
	public double FontSize { get; set; } = 20;
	public ChartDashStyle StrokeStyle { get; set; }
	public Color FontColor { get; set; }
}
