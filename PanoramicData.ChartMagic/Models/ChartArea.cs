using PanoramicData.ChartMagic.Interfaces;

namespace PanoramicData.ChartMagic.Models
{
	public class ChartArea : ChartNamedElement
	{
		public ChartArea(IChartElement parent, string name) : base(parent, name)
		{
			InnerPlot = new InnerChartArea(this, "InnerPlot");

			XAxis = new AxisArea(this, "XAxis")
			{
				Alignment = AxisAlignment.Top,
				IsEnabled = true
			};

			YAxis = new AxisArea(this, "YAxis")
			{
				Alignment = AxisAlignment.Right,
				IsEnabled = true
			};

			XAxis2Area = new AxisArea(this, "X2Axis")
			{
				Alignment = AxisAlignment.Bottom
			};

			YAxis2Area = new AxisArea(this, "Y2Axis")
			{
				Alignment = AxisAlignment.Left
			};
		}

		public InnerChartArea InnerPlot { get; }

		public AxisArea XAxis { get; }

		public AxisArea XAxis2Area { get; }

		public AxisArea YAxis { get; }

		public AxisArea YAxis2Area { get; }
	}
}
