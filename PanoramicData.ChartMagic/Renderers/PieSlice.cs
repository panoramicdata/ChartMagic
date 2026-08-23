using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// One slice of a pie or doughnut, resolved from a data point: its share of the total, where it
/// starts and ends, what colour it is and what it is called.
/// </summary>
/// <param name="Label">The text drawn on or beside the slice.</param>
/// <param name="LegendText">The legend entry for the slice.</param>
/// <param name="Value">The value of the point.</param>
/// <param name="Percentage">The share of the total, 0 to 100.</param>
/// <param name="Color">The fill colour.</param>
/// <param name="StartAngleDegrees">Where the slice starts, clockwise from twelve o clock.</param>
/// <param name="SweepAngleDegrees">How far it extends, clockwise.</param>
internal sealed record PieSlice(
	string Label,
	string LegendText,
	double Value,
	double Percentage,
	Color Color,
	double StartAngleDegrees,
	double SweepAngleDegrees)
{
	/// <summary>
	/// The angle at the middle of the slice, which is where its label belongs.
	/// </summary>
	internal double MidAngleDegrees => StartAngleDegrees + (SweepAngleDegrees / 2);
}

/// <summary>
/// Turns a pie or doughnut series into slices.
/// </summary>
internal static class PieSliceBuilder
{
	/// <summary>
	/// The fallback colour sequence, used only for a point that carries no colour of its own.
	/// </summary>
	/// <remarks>
	/// The Microsoft chart control assigns a palette colour per point for a pie. Every point
	/// arriving through DocMagic carries an explicit colour, so this is a fallback for a caller
	/// building the object model directly rather than an attempt to reproduce that palette.
	/// </remarks>
	private static readonly Color[] FallbackPalette =
	[
		Color.FromArgb(0x41, 0x72, 0xC4),
		Color.FromArgb(0xED, 0x7D, 0x31),
		Color.FromArgb(0xA5, 0xA5, 0xA5),
		Color.FromArgb(0xFF, 0xC0, 0x00),
		Color.FromArgb(0x5B, 0x9B, 0xD5),
		Color.FromArgb(0x70, 0xAD, 0x47),
		Color.FromArgb(0x26, 0x44, 0x78),
		Color.FromArgb(0x9E, 0x48, 0x0E),
		Color.FromArgb(0x63, 0x63, 0x63),
		Color.FromArgb(0x99, 0x74, 0x00)
	];

	/// <summary>
	/// Builds the slices for a series, applying the collected-slice threshold and turning values
	/// into angles.
	/// </summary>
	internal static List<PieSlice> Build(Series series)
	{
		var values = series.Points
			.Where(p => p.YValue is not null)
			.Select(p => (Point: p, Value: Math.Abs(p.YValue!.Value)))
			.Where(p => p.Value > 0)
			.ToList();

		var total = values.Sum(v => v.Value);
		if (total <= 0)
		{
			return [];
		}

		// Slices below the threshold are combined into one, as the Microsoft chart control does
		// when CollectedThreshold is set with CollectedThresholdUsePercent.
		var threshold = series.PieCollectedThresholdPercent;
		var kept = new List<(ChartPoint Point, double Value)>();
		var collectedValue = 0d;
		var collectedCount = 0;

		foreach (var entry in values)
		{
			if (threshold > 0 && entry.Value / total * 100 < threshold)
			{
				collectedValue += entry.Value;
				collectedCount++;
			}
			else
			{
				kept.Add(entry);
			}
		}

		// One slice below the threshold is left where it is: replacing a single slice with a
		// combined slice of the same size hides its identity and gains nothing.
		if (collectedCount == 1)
		{
			kept = values;
			collectedValue = 0;
		}

		var slices = new List<PieSlice>();
		var angle = series.PieStartAngleDegrees;
		var paletteIndex = 0;

		foreach (var entry in kept)
		{
			var percentage = entry.Value / total * 100;
			var sweep = percentage / 100 * 360;
			var color = entry.Point.Color ?? FallbackPalette[paletteIndex++ % FallbackPalette.Length];

			slices.Add(new PieSlice(
				Label: LabelFor(series, entry.Point, entry.Value, percentage, total),
				LegendText: LegendTextFor(entry.Point, entry.Value),
				Value: entry.Value,
				Percentage: percentage,
				Color: color,
				StartAngleDegrees: angle,
				SweepAngleDegrees: sweep));

			angle += sweep;
		}

		if (collectedValue > 0)
		{
			var percentage = collectedValue / total * 100;
			var label = series.PieCollectedLabel is { Length: > 0 } ? series.PieCollectedLabel : "Other";

			slices.Add(new PieSlice(
				Label: label,
				LegendText: label,
				Value: collectedValue,
				Percentage: percentage,
				Color: series.PieCollectedColor ?? Color.Gray,
				StartAngleDegrees: angle,
				SweepAngleDegrees: percentage / 100 * 360));
		}

		return slices;
	}

	private static string LabelFor(Series series, ChartPoint point, double value, double percentage, double total)
		=> series.PieLabelStyle == Models.PieLabelStyle.Disabled
			? string.Empty
			: Substitute(series.LabelText, point, value, percentage, total)
				?? value.ToString("0.##", CultureInfo.InvariantCulture);

	private static string LegendTextFor(ChartPoint point, double value)
		=> point.LegendText
			?? point.XValueString
			?? value.ToString("0.##", CultureInfo.InvariantCulture);

	/// <summary>
	/// Replaces the Microsoft chart keywords that appear in report templates.
	/// </summary>
	/// <remarks>
	/// The keyword set is deliberately small: these four are what appears in practice. An
	/// unrecognised keyword is left in place rather than blanked, so that it shows up as itself
	/// on the chart instead of vanishing silently.
	/// </remarks>
	private static string? Substitute(string? text, ChartPoint point, double value, double percentage, double total)
	{
		if (text is not { Length: > 0 })
		{
			return null;
		}

		return text
			.Replace("#VALX", point.XValueString ?? point.XValue.ToString("0.##", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
			.Replace("#VALY", value.ToString("0.##", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
			.Replace("#PERCENT", percentage.ToString("0.#", CultureInfo.InvariantCulture) + "%", StringComparison.OrdinalIgnoreCase)
			.Replace("#TOTAL", total.ToString("0.##", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
			.Replace("#LEGENDTEXT", point.LegendText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
	}
}
