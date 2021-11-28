namespace PanoramicData.ChartMagic.Models;

public class ChartNamedElementCollection<T> : ChartElementCollection<T>, INameController where T : ChartNamedElement
{
	internal ChartNamedElementCollection(IChartElement parent, IList<T> list) : base(parent, list)
	{
	}

	public bool IsUniqueName(string name) => FindByName(name) is null;

	public virtual T? FindByName(string name)
	{
		using (var enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				var current = enumerator.Current;
				if (current.Name == name)
				{
					return current;
				}
			}
		}

		return null;
	}
}
