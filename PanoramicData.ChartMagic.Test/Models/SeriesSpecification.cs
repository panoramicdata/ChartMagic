using PanoramicData.ChartMagic.Extensions;
using PanoramicData.ChartMagic.Models;
using System.Collections.Generic;
using System.Drawing;

namespace PanoramicData.ChartMagic.Test.Models;

public class SeriesSpecification
{
	public List<ChartPoint> Points { get; set; } = new();
	public SeriesChartType ChartType { get; set; }
	public int BorderWidth { get; set; } = 2;
	public bool IsXValueIndexed { get; set; }
	public ChartValueType XValueType { get; set; }
	public Color Color { get; set; } = Colors.Transparent;
	public Color BorderColor { get; set; }
	public string? LabelText { get; set; }
	public string? LegendText { get; set; }
	public float FontSize { get; set; } = 20;
	public ChartDashStyle BorderStyle { get; set; } = ChartDashStyle.Solid;
}
