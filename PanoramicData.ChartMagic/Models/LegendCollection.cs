using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;

namespace PanoramicData.ChartMagic.Models;

public class LegendCollection : ChartNamedElementCollection<Legend>
{
	public LegendCollection(IChartElement parent, IList<Legend> list) : base(parent, list)
	{
	}
}
