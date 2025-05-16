namespace PanoramicData.ChartMagic.Models;

public class ChartElementCollection<T>(IChartElement parent, IList<T> list) : Collection<T>(list), IChartElement where T : ChartElement
{
	public IChartElement Parent { get; } = parent;
}
