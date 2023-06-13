namespace PanoramicData.ChartMagic.Models;

public abstract class ChartElement : ChartElementBase
{
	protected ChartElement(IChartElement parent) : base(parent)
	{
	}

	public double XPositionPercent { get; set; }

	public double YPositionPercent { get; set; }

	public double XRadiusPixels { get; set; }

	public double YRadiusPixels { get; set; }

	public double WidthPercent { get; set; } = 100;

	public double HeightPercent { get; set; } = 100;

	public Color FillColor { get; set; } = Colors.Transparent;

	public Color StrokeColor { get; set; } = Colors.Transparent;

	public StrokeLineCapStyle StrokeLineCapStyle { get; set; } = StrokeLineCapStyle.Round;

	public StrokeLineJoinStyle StrokeLineJoinStyle { get; set; } = StrokeLineJoinStyle.Round;

	public double StrokeWidth { get; set; } = 2;

	public double FontSize { get; set; } = 20;

	public Color FontColor { get; set; } = Color.Black;

	public FontWeight FontWeight { get; set; }

	public string? FontFamily { get; set; }

	public ChartDashStyle StrokeStyle { get; set; }

	internal double GetCanvasXLocationPercent()
		=> Parent is ChartElement parent && !parent.IsRoot
			? XPositionPercent * parent.GetCanvasWidthPercent() / 100 + parent.GetCanvasXLocationPercent()
			: XPositionPercent;

	internal double GetCanvasYLocationPercent()
		=> Parent is ChartElement parent && !parent.IsRoot
			? YPositionPercent * parent.GetCanvasHeightPercent() / 100 + parent.GetCanvasYLocationPercent()
			: YPositionPercent;

	internal double GetCanvasWidthPercent()
		=> WidthPercent * ((Parent is ChartElement parent && !parent.IsRoot) ? parent.GetCanvasWidthPercent() / 100 : 1);

	internal double GetCanvasHeightPercent()
		=> HeightPercent * ((Parent is ChartElement parent && !parent.IsRoot) ? parent.GetCanvasHeightPercent() / 100 : 1);
}
