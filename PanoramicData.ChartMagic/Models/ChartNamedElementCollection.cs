using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;

namespace PanoramicData.ChartMagic.Models
{
	public class ChartNamedElementCollection<T> : ChartElementCollection<T>, INameController where T : ChartNamedElement
	{
		internal ChartNamedElementCollection(IChartElement parent, IList<T>? list = null) : base(parent, list)
		{
		}

		public bool IsUniqueName(string name) => FindByName(name) is null;

		public virtual T? FindByName(string name)
		{
			using (IEnumerator<T> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					if (current.Name == name)
					{
						return current;
					}
				}
			}

			return null;
		}
	}
}