using PanoramicData.ChartMagic.Interfaces;

namespace PanoramicData.ChartMagic.Models
{
	public class Legend : ChartNamedElement
	{
		public Legend(IChartElement parent, string name) : base(parent, name)
		{
		}

		public LegendStyle Style { get; set; }
	}
}