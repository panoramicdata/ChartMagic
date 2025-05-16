namespace PanoramicData.ChartMagic.Models;

public class TitleCollection(IChartElement parent, IList<Title> list) : ChartNamedElementCollection<Title>(parent, list)
{
}
