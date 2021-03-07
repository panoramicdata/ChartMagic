using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;

namespace PanoramicData.ChartMagic.Models
{
	public class SeriesCollection : ChartNamedElementCollection<Series>
	{
		public SeriesCollection(IChartElement parent) : base(parent)
		{
		}

		public SeriesCollection(IChartElement parent, IList<Series> list) : base(parent, list)
		{
		}
	}
}
