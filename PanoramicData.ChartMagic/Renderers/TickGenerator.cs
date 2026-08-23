namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// Chooses the values at which an axis is labelled and gridded.
/// </summary>
/// <remarks>
/// Issue #31: there was nowhere that decided this, because nothing drew ticks. An axis needs
/// values a reader recognises - 0, 20, 40 rather than 3.7, 23.7, 43.7 - so the step is snapped
/// to 1, 2, 2.5 or 5 times a power of ten and the first tick is aligned to a multiple of it.
/// </remarks>
internal static class TickGenerator
{
	private static readonly double[] NiceMultipliers = [1, 2, 2.5, 5, 10];

	private const int MaximumTicks = 1000;

	/// <summary>
	/// Ticks across a linear range, at a caller-supplied interval or a readable one.
	/// </summary>
	internal static IReadOnlyList<double> Linear(double min, double max, double? interval, int targetCount)
	{
		if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max) || max <= min)
		{
			return [min];
		}

		var step = interval is > 0
			? interval.Value
			: NiceStep((max - min) / Math.Max(targetCount, 1));

		// Guard against a step so small relative to the range that we would generate
		// unbounded ticks.
		if (step <= 0 || (max - min) / step > MaximumTicks)
		{
			step = NiceStep((max - min) / Math.Max(targetCount, 1));
		}

		var decimals = DecimalsFor(step);
		var ticks = new List<double>();

		// A tolerance of one part in a billion of the step, so a tick that lands exactly on
		// the maximum is not dropped by floating-point drift.
		var tolerance = step * 1e-9;
		for (var value = Math.Ceiling(min / step) * step; value <= max + tolerance; value += step)
		{
			ticks.Add(Math.Round(value, decimals));
			if (ticks.Count >= MaximumTicks)
			{
				break;
			}
		}

		return ticks.Count == 0 ? [min] : ticks;
	}

	/// <summary>
	/// Decade ticks across a logarithmic range, with the intermediate 2..9 subdivisions when
	/// the range is narrow enough for them to be legible.
	/// </summary>
	internal static IReadOnlyList<double> Logarithmic(double min, double max, bool includeMinor)
	{
		var low = min > 0 ? min : max > 0 ? max / 1000 : 1;
		var high = max > low ? max : low * 10;

		var firstExponent = (int)Math.Floor(Math.Log10(low));
		var lastExponent = (int)Math.Ceiling(Math.Log10(high));

		var ticks = new List<double>();
		for (var exponent = firstExponent; exponent <= lastExponent; exponent++)
		{
			var decade = Math.Pow(10, exponent);
			ticks.Add(decade);

			if (!includeMinor || lastExponent - firstExponent > 4)
			{
				continue;
			}

			for (var multiplier = 2; multiplier <= 9; multiplier++)
			{
				var value = decade * multiplier;
				if (value < high)
				{
					ticks.Add(value);
				}
			}
		}

		ticks.Sort();
		return ticks;
	}

	/// <summary>
	/// Snaps a raw step up to the nearest 1, 2, 2.5 or 5 times a power of ten.
	/// </summary>
	private static double NiceStep(double rawStep)
	{
		if (rawStep <= 0)
		{
			return 1;
		}

		var exponent = Math.Floor(Math.Log10(rawStep));
		var magnitude = Math.Pow(10, exponent);
		var normalised = rawStep / magnitude;

		foreach (var multiplier in NiceMultipliers)
		{
			if (normalised <= multiplier)
			{
				return multiplier * magnitude;
			}
		}

		return 10 * magnitude;
	}

	/// <summary>
	/// How many decimal places a tick at this step needs, so that a step of 0.25 labels as
	/// 0.25 and a step of 20 does not label as 20.00.
	/// </summary>
	internal static int DecimalsFor(double step)
	{
		if (step <= 0)
		{
			return 0;
		}

		var decimals = (int)Math.Ceiling(-Math.Log10(step)) + 1;
		return Math.Clamp(decimals, 0, 10);
	}
}
