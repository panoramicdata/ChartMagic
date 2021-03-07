using PanoramicData.ChartMagic.Models;
using System;
using System.Linq;

namespace PanoramicData.ChartMagic.Renderers
{
	internal class AxisHandler
	{
		private Chart _chart;

		public AxisHandler(Chart chart)
		{
			_chart = chart;
		}

		internal AxisHandlerResult Process()
		{
			var firstXValueType = _chart.Series[0].XValueType;
			if (_chart.Series.Any(s => s.XValueType != firstXValueType))
			{
				throw new NotSupportedException("All Series must have the same XValueType.");
			}

			var result = new AxisHandlerResult
			{
				MinYDouble = _chart.Series.Min(s => s.Points.Where(p => p.YValue is not null).Min(p => (double)p.YValue!)),
				MaxYDouble = _chart.Series.Max(s => s.Points.Where(p => p.YValue is not null).Max(p => (double)p.YValue!)),
				MaxXCount = _chart.Series.Max(s => s.Points.Count)
			};

			switch (firstXValueType)
			{
				case ChartValueType.Auto:
					break;
				case ChartValueType.Double:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (double)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (double)p.XValue!));
					break;
				case ChartValueType.Single:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (float)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (float)p.XValue!));
					break;
				case ChartValueType.Int32:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (int)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (int)p.XValue!));
					break;
				case ChartValueType.Int64:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (long)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (long)p.XValue!));
					break;
				case ChartValueType.UInt32:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (uint)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (uint)p.XValue!));
					break;
				case ChartValueType.UInt64:
					result.MinXDouble = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (ulong)p.XValue!));
					result.MaxXDouble = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (ulong)p.XValue!));
					break;
				case ChartValueType.String:
					result.MinXString = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (string)p.XValue!));
					result.MaxXString = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (string)p.XValue!));
					break;
				case ChartValueType.Date:
				case ChartValueType.DateTime:
				case ChartValueType.Time:
				case ChartValueType.DateTimeOffset:
					result.MinXDateTimeOffset = _chart.Series.Min(s => s.Points.Where(p => p.XValue is not null).Min(p => (DateTimeOffset)p.XValue!));
					result.MaxXDateTimeOffset = _chart.Series.Max(s => s.Points.Where(p => p.XValue is not null).Max(p => (DateTimeOffset)p.XValue!));
					break;
				default:
					throw new NotSupportedException($"Unsupported xAxis type {firstXValueType}");
			}

			return result;
		}
	}
}