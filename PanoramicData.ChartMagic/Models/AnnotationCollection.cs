using PanoramicData.ChartMagic.Interfaces;
using System.Collections.Generic;

namespace PanoramicData.ChartMagic.Models
{
	public class AnnotationCollection : ChartNamedElementCollection<Annotation>
	{
		public AnnotationCollection(IChartElement parent) : base(parent)
		{
		}

		public AnnotationCollection(IChartElement parent, IList<Annotation> list) : base(parent, list)
		{
		}
	}
}