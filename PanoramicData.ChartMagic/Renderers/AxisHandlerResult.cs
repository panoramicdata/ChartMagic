namespace PanoramicData.ChartMagic.Renderers;

internal class AxisHandlerResult
{
	public bool SeriesPresent { get; internal set; }
	public double? MinXDouble { get; internal set; }
	public double? MaxXDouble { get; internal set; }
	public string? MinXString { get; internal set; }
	public string? MaxXString { get; internal set; }
	public DateTimeOffset? MinXDateTimeOffset { get; internal set; }
	public DateTimeOffset? MaxXDateTimeOffset { get; internal set; }
	public double? MinYDouble { get; internal set; }
	public double? MaxYDouble { get; internal set; }
	public int? MaxXCount { get; internal set; }
}
