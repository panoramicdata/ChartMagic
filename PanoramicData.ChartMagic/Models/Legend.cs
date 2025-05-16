namespace PanoramicData.ChartMagic.Models;

public class Legend(IChartElement parent, string name) : ChartNamedElement(parent, name)
{
	public LegendStyle Style { get; set; }
}
