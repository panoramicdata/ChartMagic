using PanoramicData.ChartMagic.Models;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// What kind of editor a property needs.
/// </summary>
public enum EditorKind
{
	/// <summary>A checkbox.</summary>
	Boolean,

	/// <summary>A dropdown of the enum's values.</summary>
	Enumeration,

	/// <summary>A number box.</summary>
	Number,

	/// <summary>A colour, edited as text so that named colours can be typed.</summary>
	Colour,

	/// <summary>A text box.</summary>
	Text,

	/// <summary>Shown but not editable: a collection, or a type with no sensible editor.</summary>
	ReadOnly
}

/// <summary>
/// One editable property of a specification.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="Group">The heading it is filed under.</param>
/// <param name="Kind">The editor it needs.</param>
/// <param name="Options">The permitted values, for an enumeration.</param>
/// <param name="IsDefault">Whether it still holds the value a fresh specification would.</param>
public record PropertyEditor(
	string Name,
	string Group,
	EditorKind Kind,
	IReadOnlyList<string> Options,
	bool IsDefault);

/// <summary>
/// Reads and writes specification properties by name, so the demo can offer the whole
/// specification for editing without listing 130 properties by hand.
/// </summary>
/// <remarks>
/// Listing them by hand would go out of date the first time a property was added, and a demo
/// that silently omits a property is worse than one that shows it as unsupported.
/// </remarks>
public static class SpecificationEditor
{
	private static readonly PropertyInfo[] Properties = typeof(ChartSpecification)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(p => p.CanRead)
		.OrderBy(p => GroupOf(p.Name), StringComparer.Ordinal)
		.ThenBy(p => p.Name, StringComparer.Ordinal)
		.ToArray();

	/// <summary>
	/// Every property, in the order they should be shown.
	/// </summary>
	public static IReadOnlyList<PropertyEditor> Describe(ChartSpecification specification)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var defaults = new ChartSpecification();
		return
		[
			.. Properties.Select(p => new PropertyEditor(
				p.Name,
				GroupOf(p.Name),
				KindOf(p),
				OptionsOf(p),
				Equals(Format(p.GetValue(specification)), Format(p.GetValue(defaults)))))
		];
	}

	/// <summary>
	/// The current value of a property, as text for an input box.
	/// </summary>
	public static string Read(ChartSpecification specification, string name)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var property = Properties.FirstOrDefault(p => p.Name == name);
		return property is null ? string.Empty : Format(property.GetValue(specification));
	}

	/// <summary>
	/// Writes a property from text, and reports whether it took.
	/// </summary>
	/// <remarks>
	/// A value that will not parse is ignored rather than throwing: the user is typing into a
	/// live chart, and half-typed input is normal rather than exceptional.
	/// </remarks>
	public static bool Write(ChartSpecification specification, string name, string? value)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var property = Properties.FirstOrDefault(p => p.Name == name);
		if (property is null || !property.CanWrite)
		{
			return false;
		}

		if (!TryParse(property.PropertyType, value, out var parsed))
		{
			return false;
		}

		property.SetValue(specification, parsed);
		return true;
	}

	/// <summary>
	/// A copy of a specification, so the theme colours can be applied to something disposable
	/// rather than to the specification the user is editing.
	/// </summary>
	/// <remarks>
	/// Points are records and are shared rather than copied: nothing mutates them, and copying
	/// them per render for every sample would be waste.
	/// </remarks>
	public static ChartSpecification Clone(ChartSpecification specification)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var copy = new ChartSpecification();
		foreach (var property in Properties.Where(p => p.CanWrite))
		{
			property.SetValue(copy, property.GetValue(specification));
		}

		copy.SeriesList = [.. specification.SeriesList.Select(CloneSeries)];
		return copy;
	}

	private static SeriesSpecification CloneSeries(SeriesSpecification series)
	{
		var copy = new SeriesSpecification();
		foreach (var property in typeof(SeriesSpecification)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p is { CanRead: true, CanWrite: true }))
		{
			property.SetValue(copy, property.GetValue(series));
		}

		return copy;
	}

	/// <summary>
	/// Groups properties by what they configure, taken from the name. Grouping 130 rows is what
	/// makes them navigable; the alternative is one alphabetical wall.
	/// </summary>
	private static string GroupOf(string name) => name switch
	{
		_ when name.StartsWith("ChartArea", StringComparison.Ordinal) => "2 Chart area",
		_ when name.StartsWith("InnerPlot", StringComparison.Ordinal) => "3 Inner plot",
		_ when name.StartsWith("Legend", StringComparison.Ordinal) => "4 Legend",
		_ when name.StartsWith("XAxis", StringComparison.Ordinal) => "5 X axis",
		_ when name.StartsWith("YAxis", StringComparison.Ordinal) || name.StartsWith("UseYAxis", StringComparison.Ordinal) => "6 Y axis",
		_ when name.StartsWith("Axis", StringComparison.Ordinal) => "7 Axis, both",
		_ when name.StartsWith("Pie", StringComparison.Ordinal) || name.StartsWith("Doughnut", StringComparison.Ordinal) => "8 Pie and doughnut",
		_ when name.Contains("3d", StringComparison.Ordinal) => "9 Three dimensional",
		_ when name.StartsWith("Label", StringComparison.Ordinal) || name.StartsWith("Palette", StringComparison.Ordinal) => "A Labels and palette",
		_ when name.StartsWith("Chart", StringComparison.Ordinal) => "1 Chart",
		_ => "B Other"
	};

	private static EditorKind KindOf(PropertyInfo property)
	{
		var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
		if (!property.CanWrite || (!type.IsEnum && !IsEditableScalar(type)))
		{
			return EditorKind.ReadOnly;
		}

		if (type.IsEnum)
		{
			return EditorKind.Enumeration;
		}

		return type.Name switch
		{
			nameof(Boolean) => EditorKind.Boolean,
			nameof(Color) => EditorKind.Colour,
			nameof(Int32) or nameof(Int64) or nameof(Double) or nameof(Single) => EditorKind.Number,
			_ => EditorKind.Text
		};
	}


	private static bool IsEditableScalar(Type type) => type == typeof(bool)
		|| type == typeof(Color)
		|| type == typeof(int)
		|| type == typeof(long)
		|| type == typeof(double)
		|| type == typeof(float)
		|| type == typeof(string)
		|| type == typeof(object);

	private static IReadOnlyList<string> OptionsOf(PropertyInfo property)
	{
		var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
		if (!type.IsEnum)
		{
			return [];
		}

		var names = Enum.GetNames(type).ToList();

		// A nullable enum can be cleared, so it needs an empty option to clear it to.
		if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
		{
			names.Insert(0, string.Empty);
		}

		return names;
	}

	/// <summary>
	/// Formats a value for an input box: round-trippable, so reading and writing it back is a
	/// no-op.
	/// </summary>
	private static string Format(object? value) => value switch
	{
		null => string.Empty,
		bool flag => flag ? "true" : "false",
		Color colour => FormatColour(colour),
		double number => number.ToString("0.######", CultureInfo.InvariantCulture),
		float number => number.ToString("0.######", CultureInfo.InvariantCulture),
		string text => text,
		IEnumerable list => FormattableString.Invariant($"[{list.Cast<object?>().Count()} items]"),
		_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
	};

	/// <summary>
	/// A colour in CSS terms, which is what the colour picker reads and writes: transparent,
	/// a six-digit hex, or rgba where there is partial alpha.
	/// </summary>
	private static string FormatColour(Color colour) => colour.A switch
	{
		0 => "transparent",
		255 => FormattableString.Invariant($"#{colour.R:X2}{colour.G:X2}{colour.B:X2}"),
		_ => FormattableString.Invariant(
			$"rgba({colour.R}, {colour.G}, {colour.B}, {colour.A / 255.0:0.###})")
	};
	private static bool TryParse(Type target, string? text, out object? parsed)
	{
		var underlying = Nullable.GetUnderlyingType(target) ?? target;
		if (string.IsNullOrWhiteSpace(text))
		{
			return TryParseEmpty(target, underlying, out parsed);
		}

		return TryParseNonEmpty(underlying, text.Trim(), out parsed);
	}

	private static bool TryParseEmpty(Type target, Type underlying, out object? parsed)
	{
		parsed = null;
		if (Nullable.GetUnderlyingType(target) is null && target.IsValueType)
		{
			return false;
		}

		parsed = underlying == typeof(string) ? string.Empty : null;
		return true;
	}

	private static bool TryParseNonEmpty(Type type, string text, out object? parsed)
	{
		if (type == typeof(string) || type == typeof(object))
		{
			parsed = text;
			return true;
		}

		if (type.IsEnum)
		{
			return Enum.TryParse(type, text, ignoreCase: true, out parsed);
		}

		if (type == typeof(Color))
		{
			return TryParseColour(text, out parsed);
		}

		if (type == typeof(bool))
		{
			return TryParseBoolean(text, out parsed);
		}

		return TryParseNumber(type, text, out parsed);
	}

	private static bool TryParseBoolean(string text, out object? parsed)
	{
		var succeeded = bool.TryParse(text, out var value);
		parsed = succeeded ? value : null;
		return succeeded;
	}

	private static bool TryParseNumber(Type type, string text, out object? parsed)
	{
		if (type == typeof(int) || type == typeof(long))
		{
			return TryParseWholeNumber(type, text, out parsed);
		}

		double number = 0;
		var supported = type == typeof(double) || type == typeof(float);
		var succeeded = supported
			&& double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
		parsed = succeeded ? type == typeof(double) ? number : (float)number : null;
		return succeeded;
	}

	private static bool TryParseWholeNumber(Type type, string text, out object? parsed)
	{
		var succeeded = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole);
		parsed = succeeded ? type == typeof(int) ? (int)whole : whole : null;
		return succeeded;
	}

	/// <summary>
	/// Parses what the colour picker and a person are both likely to type: transparent, a
	/// three, six or eight digit hex, an rgb or rgba function, or a named colour.
	/// </summary>
	/// <remarks>
	/// Eight-digit hex is read as RRGGBBAA, the CSS order, because that is what the picker
	/// emits. Reading it as AARRGGBB would silently swap the alpha for the red channel.
	/// </remarks>
	private static bool TryParseColour(string text, out object? parsed)
	{
		if (string.Equals(text, "transparent", StringComparison.OrdinalIgnoreCase))
		{
			parsed = Color.FromArgb(0, 0, 0, 0);
			return true;
		}

		if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
		{
			return TryParseRgb(text, out parsed);
		}

		if (text.StartsWith('#'))
		{
			return TryParseHex(text[1..], out parsed);
		}

		var named = Color.FromName(text);
		parsed = named.IsKnownColor ? named : null;
		return named.IsKnownColor;
	}

	private static bool TryParseRgb(string text, out object? parsed)
	{
		parsed = null;
		var inside = text[(text.IndexOf('(', StringComparison.Ordinal) + 1)..].TrimEnd(')');
		var parts = inside.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length is < 3 or > 4
			|| !int.TryParse(parts[0], CultureInfo.InvariantCulture, out var red)
			|| !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var green)
			|| !int.TryParse(parts[2], CultureInfo.InvariantCulture, out var blue))
		{
			return false;
		}

		var alpha = 1.0;
		if (parts.Length == 4
			&& !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out alpha))
		{
			return false;
		}

		parsed = Color.FromArgb(
			(int)Math.Round(Math.Clamp(alpha, 0, 1) * 255),
			Math.Clamp(red, 0, 255),
			Math.Clamp(green, 0, 255),
			Math.Clamp(blue, 0, 255));
		return true;
	}

	private static bool TryParseHex(string hex, out object? parsed)
	{
		if (hex.Length == 3)
		{
			hex = string.Concat(hex.Select(character => new string(character, 2)));
		}

		if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
		{
			parsed = null;
			return false;
		}

		parsed = hex.Length switch
		{
			6 => Color.FromArgb(255, (int)((value >> 16) & 0xFF), (int)((value >> 8) & 0xFF), (int)(value & 0xFF)),
			8 => Color.FromArgb((int)(value & 0xFF), (int)((value >> 24) & 0xFF), (int)((value >> 16) & 0xFF), (int)((value >> 8) & 0xFF)),
			_ => null
		};
		return parsed is not null;
	}

	/// <summary>
	/// Whether a property still holds the value a fresh specification would.
	/// </summary>
	public static bool IsDefault(ChartSpecification specification, string name)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var property = Properties.FirstOrDefault(p => p.Name == name);
		return property is not null
			&& Equals(Format(property.GetValue(specification)), Format(property.GetValue(new ChartSpecification())));
	}

	/// <summary>
	/// Sets a property to a typed value, bypassing the text parsing.
	/// </summary>
	public static void WriteValue(ChartSpecification specification, string name, object value)
	{
		ArgumentNullException.ThrowIfNull(specification);

		Properties.FirstOrDefault(p => p.Name == name && p.CanWrite)?.SetValue(specification, value);
	}

	/// <summary>
	/// Puts one property back to the value a fresh specification would carry.
	/// </summary>
	public static bool ResetToDefault(ChartSpecification specification, string name)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var property = Properties.FirstOrDefault(p => p.Name == name);
		if (property is null || !property.CanWrite)
		{
			return false;
		}

		property.SetValue(specification, property.GetValue(new ChartSpecification()));
		return true;
	}
}
