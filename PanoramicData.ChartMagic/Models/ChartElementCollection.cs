using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PanoramicData.ChartMagic.Models;

public class ChartElementCollection<T> : Collection<T>, IChartElement where T : ChartElement
{
	public IChartElement Parent { get; }

	public ChartElementCollection(IChartElement parent, IList<T> list) : base(list)
	{
		Parent = parent;
	}
}
