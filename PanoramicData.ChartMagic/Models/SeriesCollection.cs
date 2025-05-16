namespace PanoramicData.ChartMagic.Models;

public class SeriesCollection(IChartElement parent, IList<Series> list) : ChartNamedElementCollection<Series>(parent, list)
{
}
