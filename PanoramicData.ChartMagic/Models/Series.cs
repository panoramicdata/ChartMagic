using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

public class Series(ChartElement parent, string name) : ChartNamedElement(parent, name)
{
	public List<ChartPoint> Points { get; set; } = [];
	public SeriesChartType ChartType { get; set; }
	public bool IsXValueIndexed { get; set; }
	public string? LabelText { get; set; }
	public string? LegendText { get; set; }
	public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.None;
	public Color? MarkerStrokeColor { get; set; }
	public Color? MarkerFillColor { get; set; }
	public double? MarkerStrokeWidth { get; set; }
	public double? MarkerSize { get; set; }

	/// <summary>
	/// The radius of the hole in a doughnut, as a percentage of the outer radius. Null takes
	/// the Microsoft chart control default of 60.
	/// </summary>
	public double? DoughnutRadiusPercent { get; set; }

	/// <summary>Where slice labels are drawn on a pie or doughnut.</summary>
	public PieLabelStyle PieLabelStyle { get; set; }

	/// <summary>The colour of the line between a slice and its label, when drawn outside.</summary>
	public Color PieLineColor { get; set; } = Color.Black;

	/// <summary>
	/// Where the first slice starts, in degrees clockwise from twelve o clock.
	/// </summary>
	public double PieStartAngleDegrees { get; set; }

	/// <summary>
	/// Slices smaller than this percentage of the total are combined into a single slice.
	/// Zero disables it.
	/// </summary>
	public double PieCollectedThresholdPercent { get; set; }

	/// <summary>The colour of the combined slice.</summary>
	public Color? PieCollectedColor { get; set; }

	/// <summary>The label and legend entry for the combined slice.</summary>
	public string? PieCollectedLabel { get; set; }
}
