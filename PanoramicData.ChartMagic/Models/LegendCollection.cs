namespace PanoramicData.ChartMagic.Models;

public class LegendCollection : ChartNamedElementCollection<Legend>
{
	public LegendCollection(IChartElement parent, IList<Legend> list) : base(parent, list)
	{
	}
}
