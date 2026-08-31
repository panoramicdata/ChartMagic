namespace PanoramicData.ChartMagic.Extensions;

internal static class XmlElementExtensions
{
	internal static void SetStyle(this XmlElement xmlElement, ChartNamedElement element, bool applyFill = true, bool applyStroke = true)
	{
		var style = new List<string>();
		AddFillStyle(style, element, applyFill);
		AddStrokeStyle(style, element, applyStroke);
		xmlElement.SetAttribute("style", string.Join(";", style));
	}

	private static void AddFillStyle(List<string> style, ChartNamedElement element, bool applyFill)
	{
		if (applyFill && element.FillColor != Colors.Transparent)
		{
			style.Add($"fill:{element.FillColor.ToHex()}");
			if (element.FillColor.A != 255)
			{
				// Issue #35: fill-opacity, not opacity. The element form fades the stroke along with
				// the fill, so a translucent background could not keep a solid border - and a
				// translucent chart background could not show the page through while staying framed.
				style.Add($"fill-opacity:{(element.FillColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture)}");
			}
		}
		else
		{
			style.Add("fill:none");
		}
	}

	private static void AddStrokeStyle(List<string> style, ChartNamedElement element, bool applyStroke)
	{
		if (applyStroke && element.StrokeColor != Colors.Transparent && element.StrokeWidth != 0)
		{
			style.Add($"stroke:{element.StrokeColor.ToHex()}");
			if (element.StrokeColor.A != 255)
			{
				style.Add($"stroke-opacity:{(element.StrokeColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture)}");
			}

			var dashArray = element.StrokeStyle switch
			{
				ChartDashStyle.Dash => "5,2",
				ChartDashStyle.DashDot => "5,2,1,2",
				ChartDashStyle.DashDotDot => "5,2,1,2,1,2",
				ChartDashStyle.Dot => "5,2",
				_ => null
			};
			if (dashArray is not null)
			{
				style.Add($"stroke-dasharray:{dashArray}");
			}

			var lineCap = element.StrokeLineCapStyle switch
			{
				StrokeLineCapStyle.Square => "square",
				StrokeLineCapStyle.Round => "round",
				_ => null
			};
			if (lineCap is not null)
			{
				style.Add($"stroke-linecap:{lineCap}");
			}

			var lineJoin = element.StrokeLineJoinStyle switch
			{
				StrokeLineJoinStyle.Arcs => "arcs",
				StrokeLineJoinStyle.Bevel => "bevel",
				StrokeLineJoinStyle.MiterClip => "miter-clip",
				StrokeLineJoinStyle.Round => "round",
				_ => null
			};
			if (lineJoin is not null)
			{
				style.Add($"stroke-linejoin:{lineJoin}");
			}

			style.Add($"stroke-width:{element.StrokeWidth.ToString(CultureInfo.InvariantCulture)}");
		}
	}
}
