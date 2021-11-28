using PanoramicData.ChartMagic.Interfaces;

namespace PanoramicData.ChartMagic.Models;

public class ChartNamedElement : ChartElement
{
	internal ChartNamedElement(IChartElement parent, string name) : base(parent)
	{
		Name = name;
	}

	public string Name { get; }
}
