namespace PanoramicData.ChartMagic.Models;

public class TitleCollection : ChartNamedElementCollection<Title>
{
	public TitleCollection(IChartElement parent, IList<Title> list) : base(parent, list)
	{
	}
}
