namespace PanoramicData.ChartMagic.Extensions;

internal static class XmlElementExtensions
{
	internal static void SetStyle(this XmlElement xmlElement, ChartNamedElement element, bool applyFill = true, bool applyStroke = true)
	{
		var style = new List<string>();
		if (applyFill && element.FillColor != Colors.Transparent)
		{
			style.Add($"fill:{element.FillColor.ToHex()}");
			if (element.FillColor.A != 255)
			{
				style.Add($"opacity:{(element.FillColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture)}");
			}
		}
		else
		{
			style.Add($"fill:none");
		}
		if (applyStroke && element.StrokeColor != Colors.Transparent && element.StrokeWidth != 0)
		{
			style.Add($"stroke:{element.StrokeColor.ToHex()}");
			if (element.StrokeColor.A != 255)
			{
				style.Add($"stroke-opacity:{(element.StrokeColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture)}");
			}
			switch (element.StrokeStyle)
			{
				case ChartDashStyle.Dash:
					style.Add("stroke-dasharray:5,2");
					break;
				case ChartDashStyle.DashDot:
					style.Add("stroke-dasharray:5,2,1,2");
					break;
				case ChartDashStyle.DashDotDot:
					style.Add("stroke-dasharray:5,2,1,2,1,2");
					break;
				case ChartDashStyle.Dot:
					style.Add("stroke-dasharray:5,2");
					break;
			}
			switch (element.StrokeLineCapStyle)
			{
				case StrokeLineCapStyle.Square:
					style.Add("stroke-linecap:square");
					break;
				case StrokeLineCapStyle.Round:
					style.Add("stroke-linecap:round");
					break;
			}
			switch (element.StrokeLineJoinStyle)
			{
				case StrokeLineJoinStyle.Arcs:
					style.Add("stroke-linejoin:arcs");
					break;
				case StrokeLineJoinStyle.Bevel:
					style.Add("stroke-linejoin:bevel");
					break;
				case StrokeLineJoinStyle.MiterClip:
					style.Add("stroke-linejoin:miter-clip");
					break;
				case StrokeLineJoinStyle.Round:
					style.Add("stroke-linejoin:round");
					break;
			}
			style.Add($"stroke-width:{element.StrokeWidth.ToString(CultureInfo.InvariantCulture)}");
		}
		xmlElement.SetAttribute("style", string.Join(";", style));
	}
}
