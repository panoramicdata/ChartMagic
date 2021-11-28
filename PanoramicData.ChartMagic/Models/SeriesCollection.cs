namespace PanoramicData.ChartMagic.Models;

public class SeriesCollection : ChartNamedElementCollection<Series>
{
	public SeriesCollection(IChartElement parent, IList<Series> list) : base(parent, list)
	{
	}
}
