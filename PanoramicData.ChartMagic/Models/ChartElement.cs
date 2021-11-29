namespace PanoramicData.ChartMagic.Models;

public abstract class ChartElement : ChartElementBase
{
	internal ChartElement(IChartElement parent) : base(parent)
	{
	}

	public double XPositionPercent { get; set; }
	public double YPositionPercent { get; set; }
	public double XRadiusPixels { get; set; }
	public double YRadiusPixels { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
	public Color FillColor { get; set; } = Colors.Transparent;
	public Color StrokeColor { get; set; } = Colors.Transparent;
	public StrokeLineCapStyle StrokeLineCapStyle { get; set; } = StrokeLineCapStyle.Round;
	public StrokeLineJoinStyle StrokeLineJoinStyle { get; set; } = StrokeLineJoinStyle.Round;
	public double StrokeWidth { get; set; } = 2;
	public double FontSize { get; set; } = 20;
	public ChartDashStyle StrokeStyle { get; set; }
}
