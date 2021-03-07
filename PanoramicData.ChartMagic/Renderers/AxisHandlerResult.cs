using System;

namespace PanoramicData.ChartMagic.Renderers
{
	internal class AxisHandlerResult
	{
		public double? MinXDouble { get; set; }
		public double? MaxXDouble { get; set; }
		public string? MinXString { get; set; }
		public string? MaxXString { get; set; }
		public DateTimeOffset MinXDateTimeOffset { get; internal set; }
		public DateTimeOffset MaxXDateTimeOffset { get; internal set; }
		public double MinYDouble { get; internal set; }
		public double MaxYDouble { get; internal set; }
		public int MaxXCount { get; internal set; }
	}
}