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
		}

		result.MinY = chart.Series.Min(s => s.Points.Where(p => p.YValue is not null).Min(p => (double)p.YValue!));
		result.MaxXCount = chart.Series.Max(s => s.Points.Count);

		result.MaxY = new[] {
			chart.Series.Max(s => s.Points.Where(p => p.YValue is not null).Max(p => (double)p.YValue!)),
			GetMaxY(SeriesChartType.StackedArea),
			GetMaxY(SeriesChartType.StackedColumn),
			GetMaxY(SeriesChartType.StackedBar)}
			.Max();

		result.MinX = chart.Series.Min(s => s.Points.Min(p => p.XValue!));
		result.MaxX = chart.Series.Max(s => s.Points.Max(p => p.XValue!));

		// No padding here any more.
		//
		// This used to widen the Y range by 2.5% at each end, and the X range by a fraction of
		// a category. Both fought the axis bound selection downstream: a data maximum of 30
		// became 30.75, which pushed the chosen interval from 5 to 10 and produced four labels
		// where the Microsoft chart control produces eight. PlotGeometry derives the displayed
		// bounds from the raw data now, so what this returns has to be the raw data.

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
