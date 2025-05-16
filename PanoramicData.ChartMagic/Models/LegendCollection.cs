namespace PanoramicData.ChartMagic.Models;

public class LegendCollection(IChartElement parent, IList<Legend> list) : ChartNamedElementCollection<Legend>(parent, list)
{
}
