using System.Collections.Generic;
using System.Drawing;

namespace PanoramicData.ChartMagic.Models
{
	public class Series : ChartNamedElement
	{
		public Series(ChartElement parent, string name) : base(parent, name)
		{
		}

		public List<ChartPoint> Points { get; set; } = new();
		public SeriesChartType ChartType { get; set; }
		public int BorderWidth { get; set; }
		public bool IsXValueIndexed { get; set; }
		public ChartValueType XValueType { get; set; }
		public Color Color { get; set; }
		public string? LabelText { get; set; }
		public string? LegendText { get; set; }
	}
}