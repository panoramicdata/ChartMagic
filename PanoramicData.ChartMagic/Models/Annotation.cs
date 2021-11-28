using PanoramicData.ChartMagic.Interfaces;

namespace PanoramicData.ChartMagic.Models;

public class Annotation : ChartNamedElement
{
	public Annotation(IChartElement parent, string name) : base(parent, name)
	{
	}
}
