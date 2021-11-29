namespace PanoramicData.ChartMagic.Renderers;

internal class AxisHandlerResult
{
	public bool SeriesPresent { get; internal set; }
	public double? MinX { get; internal set; }
	public double? MaxX { get; internal set; }
	public double? MinY { get; internal set; }
	public double? MaxY { get; internal set; }
	public int? MaxXCount { get; internal set; }
}
