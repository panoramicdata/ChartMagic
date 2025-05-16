namespace PanoramicData.ChartMagic.Renderers;

internal class AxisHandler(Chart chart)
{
	internal AxisHandlerResult Process()
	{
		var result = new AxisHandlerResult();
		if (chart.Series.Count == 0)
		{
			// Nothing to do
			result.SeriesPresent = false;
			return result;
		};

		result.MinY = chart.Series.Min(s => s.Points.Where(p => p.YValue is not null).Min(p => (double)p.YValue!));
		result.MaxXCount = chart.Series.Max(s => s.Points.Count);

		result.MaxY = new[] {
			chart.Series.Max(s => s.Points.Where(p => p.YValue is not null).Max(p => (double)p.YValue!)),
			GetMaxY(SeriesChartType.StackedArea),
			GetMaxY(SeriesChartType.StackedColumn)}
			.Max();

		result.MinX = chart.Series.Min(s => s.Points.Min(p => p.XValue!));
		result.MaxX = chart.Series.Max(s => s.Points.Max(p => p.XValue!));

		// Apply max range corrections unless explicity set
		var xRange = (result.MinX ?? result.MaxX) is null ? 0 : (result.MaxX! - result.MinX!).Value;
		var yRange = (result.MinY ?? result.MaxY) is null ? 0 : (result.MaxY! - result.MinY!).Value;
		var anyTextXAxisValues = chart.Series.SelectMany(s => s.Points).Any(p => p.XValueString is not null);
		if (chart.ChartArea.XAxis.Min is null && result.MinX != 0 && anyTextXAxisValues)
		{
			result.MinX -= 1 / xRange;
		}
		if (chart.ChartArea.XAxis.Max is null && result.MaxX != 0 && anyTextXAxisValues)
		{
			result.MaxX += 1 / xRange;
		}
		if (chart.ChartArea.YAxis.Min is null && result.MinY != 0)
		{
			result.MinY -= yRange * 0.025;
		}
		if (chart.ChartArea.YAxis.Max is null && result.MaxY != 0)
		{
			result.MaxY += yRange * 0.025;
		}
		return result;
	}

	private double GetMaxY(SeriesChartType seriesChartType)
	{
		var stackedColumnDictionary = new Dictionary<string, double>();
		foreach (var point in chart.Series.Where(s => s.ChartType == seriesChartType).SelectMany(s => s.Points).Where(p => p.YValue is not null))
		{
			var xString = point.XValue.ToString() ?? string.Empty;
			if (!stackedColumnDictionary.ContainsKey(xString))
			{
				stackedColumnDictionary[xString] = point.YValue!.Value;
			}
			else
			{
				stackedColumnDictionary[xString] += point.YValue!.Value;
			}
		}
		return stackedColumnDictionary.Values.Count == 0 ? 0 : stackedColumnDictionary.Values.Max();
	}
}
