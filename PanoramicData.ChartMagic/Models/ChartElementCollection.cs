using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PanoramicData.ChartMagic.Models
{
	public class ChartElementCollection<T> : Collection<T>, IChartElement where T : ChartElement
	{
		private IChartElement _parent;

		public ChartElementCollection(IChartElement parent, IList<T>? list)
		{
			_parent = parent;
			if (list is not null)
			{
				foreach (var item in list)
				{
					Add(item);
				}
			}
		}
	}
}