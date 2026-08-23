using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

/// <summary>
/// One data point.
/// </summary>
/// <param name="XValueString">The label for the point, where the axis is categorical.</param>
/// <param name="XValue">The position of the point along the X axis.</param>
/// <param name="YValue">The value of the point, or null where there is no value.</param>
/// <param name="Color">
/// The colour of this point specifically, overriding the colour of its series. Pie and doughnut
/// charts colour every slice separately, so this is how they are coloured; for other chart types
/// it is normally left null and the series colour used.
/// </param>
/// <param name="LegendText">
/// The legend entry for this point. Only pie and doughnut charts give each point its own legend
/// entry; elsewhere the legend describes the series.
/// </param>
public record ChartPoint(
	string? XValueString,
	double XValue,
	double? YValue,
	Color? Color = null,
	string? LegendText = null);
