using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

public abstract class ChartElement(IChartElement parent) : ChartElementBase(parent)
{
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
		=> Parent is ChartElement parentElement && !parentElement.IsRoot
			? XPositionPercent * parentElement.GetCanvasWidthPercent() / 100 + parentElement.GetCanvasXLocationPercent()
			: XPositionPercent;

	internal double GetCanvasYLocationPercent()
		=> Parent is ChartElement parentElement && !parentElement.IsRoot
			? YPositionPercent * parentElement.GetCanvasHeightPercent() / 100 + parentElement.GetCanvasYLocationPercent()
			: YPositionPercent;

	internal double GetCanvasWidthPercent()
		=> WidthPercent * ((Parent is ChartElement parentElement && !parentElement.IsRoot) ? parentElement.GetCanvasWidthPercent() / 100 : 1);

	internal double GetCanvasHeightPercent()
		=> HeightPercent * ((Parent is ChartElement parentElement && !parentElement.IsRoot) ? parentElement.GetCanvasHeightPercent() / 100 : 1);
}
