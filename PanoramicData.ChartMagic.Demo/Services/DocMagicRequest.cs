using PanoramicData.ChartMagic.Models;
using System.Buffers;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// Projects a <see cref="ChartSpecification"/> into the JSON body the DocMagic chart endpoint
/// expects, so the same chart can be rendered by both and the two compared.
/// </summary>
/// <remarks>
/// The two specifications describe the same thing and were named to match, but only 72 of their
/// 114 properties share a name and their vertical positions run in opposite directions, so a
/// straight serialisation would describe a different chart. This holds the differences in one
/// place.
///
/// Nothing here is used by the library, and the demo deliberately takes no dependency on the
/// DocMagic assemblies - the body is written as JSON directly. That keeps a comparison tool out of
/// the shipping library, and it means this can be pointed at a server built from any branch.
///
/// Wire-format decisions, each established against a live server rather than assumed - the
/// first guesses were wrong, which is why they are spelled out:
///
/// <list type="bullet">
/// <item>
/// The endpoint deserialises with System.Text.Json, not with the Newtonsoft serialiser its own
/// client writes with. The two are not symmetrical, so what that client emits is no guide to
/// what the server accepts.
/// </item>
/// <item>
/// Colours are hex strings - "#RRGGBB", or "#AARRGGBB" where alpha matters. The server has a
/// colour converter that reads names and hex. It does not read the "R, G, B" form its own
/// client happens to write.
/// </item>
/// <item>
/// Enumerations are numbers. No string-enumeration converter is registered, so a name is
/// rejected outright - which is how this was found. The numbers cannot be taken from this
/// library's enumerations either, because the two do not agree: Column is 9 there and 0 here.
/// They are looked up by name in the tables below, and an unmapped name throws rather than
/// defaulting, so a divergence shows up as a failure and not as a quietly different chart.
/// </item>
/// </list>
/// </remarks>
public static class DocMagicRequest
{
	/// <summary>
	/// PNG, in the endpoint's image format enumeration.
	/// </summary>
	private const int PngImageFormat = 1;

	/// <summary>
	/// The endpoint's numeric value for each enumeration member, by enumeration and member name.
	/// </summary>
	/// <remarks>
	/// A snapshot of the enumerations in DocMagic.Data.Charting, which mirror the Microsoft chart
	/// control and so do not move. Held by name rather than by ordinal because the two libraries
	/// order theirs differently - Column is 9 there and 0 here - and a wrong number renders a
	/// different chart type without complaint, which is the worst outcome for a comparison tool.
	/// </remarks>
	private static readonly Dictionary<string, Dictionary<string, int>> EnumValues = new(StringComparer.Ordinal)
	{
		["ChartDashStyle"] = new(StringComparer.Ordinal)
		{
			["NotSet"] = 0, ["Dash"] = 1, ["DashDot"] = 2, ["DashDotDot"] = 3, ["Dot"] = 4, ["Solid"] = 5
		},
		["LegendStyle"] = new(StringComparer.Ordinal)
		{
			["Column"] = 0, ["Row"] = 1, ["Table"] = 2
		},
		["MarkerStyle"] = new(StringComparer.Ordinal)
		{
			["None"] = 0, ["Square"] = 1, ["Circle"] = 2, ["Diamond"] = 3, ["Triangle"] = 4,
			["Cross"] = 5, ["Star4"] = 6, ["Star5"] = 7, ["Star6"] = 8, ["Star10"] = 9
		},
		["IntervalAutoMode"] = new(StringComparer.Ordinal)
		{
			["FixedCount"] = 0, ["VariableCount"] = 1
		},
		["DateTimeIntervalType"] = new(StringComparer.Ordinal)
		{
			["Auto"] = 0, ["Number"] = 1, ["Years"] = 2, ["Months"] = 3, ["Weeks"] = 4, ["Days"] = 5,
			["Hours"] = 6, ["Minutes"] = 7, ["Seconds"] = 8, ["Milliseconds"] = 9, ["NotSet"] = 10
		},
		["LabelAutoFitStyles"] = new(StringComparer.Ordinal)
		{
			["None"] = 0, ["IncreaseFont"] = 1, ["DecreaseFont"] = 2, ["StaggeredLabels"] = 4,
			["LabelsAngleStep30"] = 8, ["LabelsAngleStep45"] = 16, ["LabelsAngleStep90"] = 32,
			["WordWrap"] = 64
		},
		["ChartValueType"] = new(StringComparer.Ordinal)
		{
			["Auto"] = 0, ["Double"] = 1, ["Single"] = 2, ["Int32"] = 3, ["Int64"] = 4, ["UInt32"] = 5,
			["UInt64"] = 6, ["String"] = 7, ["DateTime"] = 8, ["Date"] = 9, ["Time"] = 10,
			["DateTimeOffset"] = 11
		},
		["SeriesChartType"] = new(StringComparer.Ordinal)
		{
			["Point"] = 0, ["FastPoint"] = 1, ["Line"] = 2, ["Spline"] = 3, ["StepLine"] = 4,
			["FastLine"] = 5, ["Bar"] = 6, ["StackedBar"] = 7, ["StackedBar100"] = 8, ["Column"] = 9,
			["StackedColumn"] = 10, ["StackedColumn100"] = 11, ["Area"] = 12, ["SplineArea"] = 13,
			["StackedArea"] = 14, ["StackedArea100"] = 15, ["Pie"] = 16, ["Doughnut"] = 17,
			["Radar"] = 18, ["Polar"] = 19, ["BoxPlot"] = 20, ["Funnel"] = 21, ["Pyramid"] = 22
		},
	};

	/// <summary>
	/// Properties whose name differs between the two specifications.
	/// </summary>
	private static readonly Dictionary<string, string> Renamed = new(StringComparer.Ordinal)
	{
		[nameof(ChartSpecification.ChartAreaXPositionPercent)] = "ChartAreaXPosition",
		[nameof(ChartSpecification.ChartAreaYPositionPercent)] = "ChartAreaYPosition",
		[nameof(ChartSpecification.InnerPlotXPositionPercent)] = "InnerPlotXPosition",
		[nameof(ChartSpecification.InnerPlotYPositionPercent)] = "InnerPlotYPosition",
		[nameof(ChartSpecification.LegendXPositionPercent)] = "LegendXPosition",
		[nameof(ChartSpecification.LegendYPositionPercent)] = "LegendYPosition",
		[nameof(ChartSpecification.ChartBorderColor)] = "ChartBorderLineColor",
		[nameof(ChartSpecification.ChartBorderWidth)] = "ChartBorderLineWidth",
		[nameof(ChartSpecification.XAxisMajorGridColor)] = "XAxisMajorGridLineColor",
		[nameof(ChartSpecification.XAxisMinorGridColor)] = "XAxisMinorGridLineColor",
		[nameof(ChartSpecification.YAxisMajorGridColor)] = "YAxisMajorGridLineColor",
		[nameof(ChartSpecification.YAxisMinorGridColor)] = "YAxisMinorGridLineColor",

		// The other renderer paints the chart-area colour over the inner plot rather than over the
		// whole area, which is why this library keeps the setting on the plot.
		[nameof(ChartSpecification.InnerPlotBackgroundColor)] = "ChartAreaBackgroundColor",
	};

	/// <summary>
	/// Properties that have no counterpart, and why. Reported alongside the comparison so a
	/// difference caused by something simply not sent is not mistaken for a rendering difference.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> NotSent =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			[nameof(ChartSpecification.AnnotationList)] = "the endpoint has fixed lines rather than annotations",
			[nameof(ChartSpecification.ColumnBandFillFraction)] = "expressed per series as PointWidth, not per chart",
			[nameof(ChartSpecification.PieSweepAngleDegrees)] = "no counterpart",
			[nameof(ChartSpecification.ChartAreaXRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.ChartAreaYRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.InnerPlotXRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.InnerPlotYRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.LegendXRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.LegendYRadiusPixels)] = "rounded corners are not a concept there",
			[nameof(ChartSpecification.ChartAreaBorderColor)] = "no counterpart",
			[nameof(ChartSpecification.ChartAreaBorderWidth)] = "no counterpart",
			[nameof(ChartSpecification.ChartAreaBorderLineDashStyle)] = "no counterpart",
			[nameof(ChartSpecification.InnerPlotBorderColor)] = "no counterpart",
			[nameof(ChartSpecification.InnerPlotBorderWidth)] = "no counterpart",
			[nameof(ChartSpecification.InnerPlotBorderLineDashStyle)] = "no counterpart",
			[nameof(ChartSpecification.LegendBorderWidth)] = "no counterpart",
			[nameof(ChartSpecification.LegendBorderLineDashStyle)] = "no counterpart",
			[nameof(ChartSpecification.LegendFontColor)] = "no counterpart",
			[nameof(ChartSpecification.XAxisBackgroundColor)] = "no counterpart",
			[nameof(ChartSpecification.YAxisBackgroundColor)] = "no counterpart",
			[nameof(ChartSpecification.InnerPlotFontSize)] = "no counterpart; LabelFontSize is sent instead",
		};

	/// <summary>
	/// The vertical positions that have to be turned upside down, paired with the property giving
	/// the height of the thing being positioned.
	/// </summary>
	/// <remarks>
	/// This library measures Y from the bottom of the container and the endpoint from the top, so
	/// copying the number across describes a different rectangle unless the element fills its
	/// container. Getting this wrong is not subtle: a legend across the top arrives across the
	/// bottom.
	/// </remarks>
	private static readonly Dictionary<string, string> FlippedAgainst = new(StringComparer.Ordinal)
	{
		[nameof(ChartSpecification.ChartAreaYPositionPercent)] = nameof(ChartSpecification.ChartAreaHeightPercent),
		[nameof(ChartSpecification.InnerPlotYPositionPercent)] = nameof(ChartSpecification.InnerPlotHeightPercent),
		[nameof(ChartSpecification.LegendYPositionPercent)] = nameof(ChartSpecification.LegendHeightPercent),
	};

	/// <summary>
	/// Builds the request body for a chart at the given size.
	/// </summary>
	public static string Build(ChartSpecification specification, int widthPixels, int heightPixels)
	{
		ArgumentNullException.ThrowIfNull(specification);

		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartObject();

			// The size is a render argument here and a property there.
			writer.WriteNumber("ChartWidth", widthPixels);
			writer.WriteNumber("ChartHeight", heightPixels);
			writer.WriteNumber("ImageFormat", PngImageFormat);

			foreach (var property in typeof(ChartSpecification).GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (!property.CanRead
					|| NotSent.ContainsKey(property.Name)
					|| property.Name == nameof(ChartSpecification.SeriesList))
				{
					continue;
				}

				var name = Renamed.TryGetValue(property.Name, out var renamed) ? renamed : property.Name;
				var value = FlippedAgainst.TryGetValue(property.Name, out var heightProperty)
					? Flip(specification, property, heightProperty)
					: property.GetValue(specification);

				WriteNamed(writer, name, value);
			}

			// The axis label colour is one setting here and one per axis there.
			WriteNamed(writer, "XAxisFontColor", specification.AxisLabelColor);
			WriteNamed(writer, "YAxisFontColor", specification.AxisLabelColor);

			WriteSeries(writer, specification);
			WriteMarkerLists(writer, specification);

			writer.WriteEndObject();
		}

		return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	/// <summary>
	/// Turns a position measured from the bottom into one measured from the top.
	/// </summary>
	private static object Flip(ChartSpecification specification, PropertyInfo property, string heightProperty)
	{
		var y = Convert.ToDouble(property.GetValue(specification), CultureInfo.InvariantCulture);
		var height = Convert.ToDouble(
			typeof(ChartSpecification).GetProperty(heightProperty)!.GetValue(specification),
			CultureInfo.InvariantCulture);

		return 100 - y - height;
	}

	private static void WriteSeries(Utf8JsonWriter writer, ChartSpecification specification)
	{
		writer.WriteStartArray("SeriesList");

		var index = 0;
		foreach (var series in specification.SeriesList)
		{
			index++;
			writer.WriteStartObject();
			writer.WriteNumber("ChartType", ToEndpointValue(series.ChartType));
			writer.WriteString("Name", series.LegendText is { Length: > 0 } ? series.LegendText : $"Series {index}");
			writer.WriteBoolean("IsXValueIndexed", series.IsXValueIndexed);
			writer.WriteNumber("BorderWidth", (int)Math.Round(series.StrokeWidth));

			// One colour there, two here. A filled series carries its identity in its fill and a
			// line in its stroke, so the fill wins where there is one.
			var colour = series.FillColor.A > 0 ? series.FillColor : series.StrokeColor;
			writer.WriteString("Color", FormatColour(colour));

			if (series.LabelText is not null)
			{
				writer.WriteString("LabelText", series.LabelText);
			}

			if (series.LegendText is not null)
			{
				writer.WriteString("LegendText", series.LegendText);
			}

			WritePoints(writer, series.Points);
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
	}

	private static void WritePoints(Utf8JsonWriter writer, List<ChartPoint> points)
	{
		writer.WriteStartArray("Points");
		foreach (var point in points)
		{
			writer.WriteStartObject();
			if (point.XValueString is { Length: > 0 })
			{
				writer.WriteString("XValue", point.XValueString);
			}
			else
			{
				writer.WriteNumber("XValue", point.XValue);
			}

			WriteNamed(writer, "YValue", point.YValue);
			if (point.Color is { } pointColour)
			{
				writer.WriteString("Color", FormatColour(pointColour));
			}

			writer.WriteEndObject();
		}

		writer.WriteEndArray();
	}

	/// <summary>
	/// Marker settings, which are per series here and parallel lists on the chart there.
	/// </summary>
	private static void WriteMarkerLists(Utf8JsonWriter writer, ChartSpecification specification)
	{
		WriteMarkerStyles(writer, specification.SeriesList);
		WriteList(writer, "MarkerColors", specification.SeriesList, s => s.MarkerFillColor is { } c ? FormatColour(c) : null);
		WriteList(writer, "MarkerBorderColors", specification.SeriesList, s => s.MarkerStrokeColor is { } c ? FormatColour(c) : null);

		writer.WriteStartArray("MarkerSizes");
		foreach (var series in specification.SeriesList)
		{
			if (series.MarkerSize is { } size)
			{
				writer.WriteNumberValue((int)Math.Round(size));
			}
			else
			{
				writer.WriteNullValue();
			}
		}

		writer.WriteEndArray();

		writer.WriteStartArray("MarkerBorderWidths");
		foreach (var series in specification.SeriesList)
		{
			if (series.MarkerStrokeWidth is { } width)
			{
				writer.WriteNumberValue((int)Math.Round(width));
			}
			else
			{
				writer.WriteNullValue();
			}
		}

		writer.WriteEndArray();
	}

	private static void WriteList(
		Utf8JsonWriter writer,
		string name,
		List<SeriesSpecification> seriesList,
		Func<SeriesSpecification, string?> select)
	{
		writer.WriteStartArray(name);
		foreach (var series in seriesList)
		{
			var value = select(series);
			if (value is null)
			{
				writer.WriteNullValue();
			}
			else
			{
				writer.WriteStringValue(value);
			}
		}

		writer.WriteEndArray();
	}

	private static void WriteNamed(Utf8JsonWriter writer, string name, object? value)
	{
		switch (value)
		{
			case null:
				writer.WriteNull(name);
				break;

			case Color colour:
				writer.WriteString(name, FormatColour(colour));
				break;

			case bool flag:
				writer.WriteBoolean(name, flag);
				break;

			case string text:
				writer.WriteString(name, text);
				break;

			case Enum enumeration:
				writer.WriteNumber(name, ToEndpointValue(enumeration));
				break;

			case IEnumerable list:
				WriteArray(writer, name, list);
				break;

			default:
				writer.WriteNumber(name, Convert.ToDouble(value, CultureInfo.InvariantCulture));
				break;
		}
	}

	private static void WriteArray(Utf8JsonWriter writer, string name, IEnumerable items)
	{
		writer.WriteStartArray(name);
		foreach (var item in items)
		{
			switch (item)
			{
				case null:
					writer.WriteNullValue();
					break;
				case Color colour:
					writer.WriteStringValue(FormatColour(colour));
					break;
				case Enum enumeration:
					writer.WriteNumberValue(ToEndpointValue(enumeration));
					break;
				default:
					writer.WriteStringValue(Convert.ToString(item, CultureInfo.InvariantCulture));
					break;
			}
		}

		writer.WriteEndArray();
	}

	/// <summary>
	/// A colour as hex, one of the two forms the endpoint's colour converter reads.
	/// </summary>
	/// <remarks>
	/// Hex rather than the colour name, even where there is a name, so one code path covers every
	/// colour and alpha is never quietly dropped.
	/// </remarks>
	private static string FormatColour(Color colour)
		=> colour.A == 255
			? FormattableString.Invariant($"#{colour.R:X2}{colour.G:X2}{colour.B:X2}")
			: FormattableString.Invariant($"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}");

	/// <summary>
	/// The endpoint's numeric value for an enumeration member of this library.
	/// </summary>
	/// <exception cref="NotSupportedException">
	/// The member has no counterpart in the tables. Thrown rather than defaulted: a comparison
	/// rendered from a wrong value is worse than none, because it reads as a rendering difference.
	/// </exception>
	private static int ToEndpointValue(Enum value)
	{
		var typeName = value.GetType().Name;
		if (EnumValues.TryGetValue(typeName, out var members)
			&& members.TryGetValue(value.ToString(), out var mapped))
		{
			return mapped;
		}

		throw new NotSupportedException(
			$"{typeName}.{value} has no known value on the DocMagic chart endpoint, so the comparison "
				+ "would render something else. Add it to EnumValues.");
	}

	/// <summary>
	/// Marker styles, as the numbers the endpoint expects.
	/// </summary>
	private static void WriteMarkerStyles(Utf8JsonWriter writer, List<SeriesSpecification> seriesList)
	{
		writer.WriteStartArray("MarkerStyles");
		foreach (var series in seriesList)
		{
			writer.WriteNumberValue(ToEndpointValue(series.MarkerStyle));
		}

		writer.WriteEndArray();
	}
}
