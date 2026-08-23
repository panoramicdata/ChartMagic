using PanoramicData.ChartMagic.Models;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// One specification property the sample sets, and what it was set to.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="Value">Its value, formatted for display.</param>
public record SettingRow(string Name, string Value);

/// <summary>
/// A series, described for the reader.
/// </summary>
/// <param name="Name">The legend text, or a positional name where there is none.</param>
/// <param name="Settings">The properties this series sets.</param>
public record SeriesRow(string Name, IReadOnlyList<SettingRow> Settings);

/// <summary>
/// The data behind a sample, as a grid: one row per category, one column per series.
/// </summary>
/// <param name="SeriesNames">Column headings.</param>
/// <param name="Rows">One row per category.</param>
public record DataGrid(IReadOnlyList<string> SeriesNames, IReadOnlyList<DataGridRow> Rows);

/// <summary>
/// One row of the data grid.
/// </summary>
/// <param name="Category">The X value, labelled where the data supplied a label.</param>
/// <param name="Values">One value per series, blank where a series has no point there.</param>
public record DataGridRow(string Category, IReadOnlyList<string> Values);

/// <summary>
/// Describes a sample: which specification properties it sets, and the data behind it.
/// </summary>
/// <remarks>
/// Everything here is derived by reflection from the specification the sample actually renders,
/// rather than written out by hand beside it. Hand-written descriptions drift from the sample
/// they describe - and a demo whose captions disagree with its charts is worse than no captions,
/// because a reader cannot tell which of the two is wrong.
/// </remarks>
public static class SampleDetails
{
	/// <summary>
	/// Properties that describe the data or the layout scaffolding rather than the chart, and
	/// would only be noise in the list.
	/// </summary>
	private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
	{
		nameof(ChartSpecification.SeriesList),
		nameof(ChartSpecification.AnnotationList),
		nameof(ChartSpecification.Labels),
		nameof(ChartSpecification.Palette)
	};

	/// <summary>
	/// The properties this specification sets, being those that differ from a fresh one.
	/// </summary>
	/// <remarks>
	/// Comparing against a default instance is what makes the list short and honest: it shows
	/// what the sample asked for, not the 130 properties a specification carries. The same
	/// technique is used in Magic Suite to decide which settings a chart has actually requested.
	/// </remarks>
	public static IReadOnlyList<SettingRow> ChartSettings(ChartSpecification specification)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var defaults = new ChartSpecification();
		var rows = new List<SettingRow>();

		foreach (var property in typeof(ChartSpecification).GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (Excluded.Contains(property.Name) || !property.CanRead)
			{
				continue;
			}

			var value = property.GetValue(specification);
			var fallback = property.GetValue(defaults);

			if (AreEquivalent(value, fallback))
			{
				continue;
			}

			rows.Add(new SettingRow(property.Name, Describe(value)));
		}

		rows.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
		return rows;
	}

	/// <summary>
	/// The properties each series sets, again against a fresh one.
	/// </summary>
	public static IReadOnlyList<SeriesRow> SeriesSettings(ChartSpecification specification)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var defaults = new SeriesSpecification();
		var rows = new List<SeriesRow>();
		var index = 0;

		foreach (var series in specification.SeriesList)
		{
			index++;
			var settings = new List<SettingRow>();

			foreach (var property in typeof(SeriesSpecification).GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (property.Name == nameof(SeriesSpecification.Points) || !property.CanRead)
				{
					continue;
				}

				var value = property.GetValue(series);
				if (AreEquivalent(value, property.GetValue(defaults)))
				{
					continue;
				}

				settings.Add(new SettingRow(property.Name, Describe(value)));
			}

			rows.Add(new SeriesRow(
				series.LegendText is { Length: > 0 } ? series.LegendText : $"Series {index}",
				settings));
		}

		return rows;
	}

	/// <summary>
	/// The data, as one row per category and one column per series.
	/// </summary>
	public static DataGrid Data(ChartSpecification specification)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var names = new List<string>();
		var index = 0;
		foreach (var series in specification.SeriesList)
		{
			index++;
			names.Add(series.LegendText is { Length: > 0 } ? series.LegendText : $"Series {index}");
		}

		// Categories in the order the data presents them, so the grid reads the way the chart
		// does rather than in sorted order.
		var categories = new List<double>();
		var labels = new Dictionary<double, string>();
		foreach (var point in specification.SeriesList.SelectMany(s => s.Points))
		{
			if (!categories.Contains(point.XValue))
			{
				categories.Add(point.XValue);
			}

			if (point.XValueString is { Length: > 0 } && !labels.ContainsKey(point.XValue))
			{
				labels[point.XValue] = point.XValueString;
			}
		}

		var rows = new List<DataGridRow>();
		foreach (var category in categories)
		{
			var values = specification.SeriesList
				.Select(s => s.Points.FirstOrDefault(p => p.XValue == category))
				.Select(p => p?.YValue is { } y ? y.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty)
				.ToList();

			rows.Add(new DataGridRow(
				labels.TryGetValue(category, out var label)
					? label
					: category.ToString("0.##", CultureInfo.InvariantCulture),
				values));
		}

		return new DataGrid(names, rows);
	}

	/// <summary>
	/// Whether two property values are the same for display purposes.
	/// </summary>
	/// <remarks>
	/// Collections need comparing by content: two distinct empty lists are not equal by
	/// reference, so every list-valued property would otherwise appear in every sample.
	/// </remarks>
	private static bool AreEquivalent(object? value, object? fallback)
	{
		if (value is null || fallback is null)
		{
			return value is null && fallback is null;
		}

		if (value is string || value is not IEnumerable first || fallback is not IEnumerable second)
		{
			return Equals(value, fallback);
		}

		return first.Cast<object?>().SequenceEqual(second.Cast<object?>());
	}

	private static string Describe(object? value) => value switch
	{
		null => "null",
		Color color => DescribeColor(color),
		bool flag => flag ? "true" : "false",
		double number => number.ToString("0.####", CultureInfo.InvariantCulture),
		string text => text,
		IEnumerable list => string.Join(", ", list.Cast<object?>().Select(Describe)),
		_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
	};

	/// <summary>
	/// A colour as its name where it has one, and as hex otherwise, with the alpha shown only
	/// when it is not opaque.
	/// </summary>
	private static string DescribeColor(Color color)
	{
		if (color.A == 0)
		{
			return "transparent";
		}

		var name = color.IsNamedColor ? color.Name : FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}");
		return color.A == 255
			? name
			: FormattableString.Invariant($"{name} at {color.A * 100 / 255}% opacity");
	}
}
