using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// Writes a chart out as SVG.
/// </summary>
/// <remarks>
/// Split across several files by the part of the chart each draws: the document, the text and the
/// positioned groups live here, and the axes, the legends, the series, the pie and the markers
/// each have their own. They are one class rather than several because every part writes into the
/// same <see cref="XmlDocument"/> and shares the same output size.
/// </remarks>
internal partial class InternalSvgRenderer(int widthPixels, int heightPixels, bool debug)
{
	private readonly XmlDocument _xmlDocument = new();

	/// <summary>
	/// The font named on every text node when the chart does not name one.
	/// </summary>
	/// <remarks>
	/// Issue #60. Led by the embedded face so an SVG opened elsewhere matches our own raster
	/// output, then Arial and the generics for consumers that have neither.
	/// </remarks>
	private const string DefaultFontFamilyStack = "Liberation Sans, Arial, Helvetica, sans-serif";

	/// <summary>
	/// Gap between a tick mark and the label that belongs to it, in pixels.
	/// </summary>
	private const double TickLabelGapPixels = 4;

	internal void SaveImage(Stream stream, Chart chart)
	{
		Initialize(
			chart,
			out var defs,
			out var chartBackgroundAreaNode,
			out var chartAreaNode,
			out var innerPlotNode,
			out var axisHandlerResult);

		var geometry = new PlotGeometry(
			chart,
			axisHandlerResult,
			widthPixels * chart.ChartArea.InnerPlot.GetCanvasWidthPercent() / 100,
			heightPixels * chart.ChartArea.InnerPlot.GetCanvasHeightPercent() / 100);

		// A pie has no axes, so it takes a different path entirely: no gridlines, no axis
		// strips, and a legend that describes slices rather than series.
		var pieSeries = chart.Series.FirstOrDefault(IsPie);
		if (pieSeries is not null)
		{
			var slices = PieSliceBuilder.Build(pieSeries);
			PlotPie(pieSeries, slices, innerPlotNode, geometry.Width, geometry.Height);
			PlotPieLegend(chart, slices, chartBackgroundAreaNode);
		}
		else
		{
			// Gridlines first, so that the series are drawn over them rather than under.
			PlotGridlines(chart, geometry, innerPlotNode);

			PlotSeries(chart, geometry, defs, innerPlotNode);

			PlotAxes(chart, geometry, chartAreaNode);

			PlotLegends(chart, chartBackgroundAreaNode);
		}

		PlotAnnotations(chart, chartBackgroundAreaNode);

		// Issue #27: UTF-8, not UTF-16.
		//
		// Encoding.Unicode is UTF-16 LE. A UTF-16 SVG is valid and renders fine in a browser,
		// but Chart.SaveImage produces raster output by rendering to SVG and reloading it
		// through SKSvg, and that parse silently yields an empty picture for UTF-16 input. The
		// result was a valid PNG or JPEG containing no chart content at all - the background
		// fill and nothing else - while the SVG of the same chart was complete.
		//
		// UTF-8 is also what consumers expect of an .svg file; reading one with a UTF-8 reader
		// previously failed on the first byte.
		var writer = new XmlTextWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
		{
			Formatting = Formatting.Indented
		};
		_xmlDocument.WriteContentTo(writer);
		writer.Flush();
	}

	private void Initialize(
		Chart chart,
		out XmlElement defs,
		out XmlElement chartBackgroundAreaNode,
		out XmlElement chartAreaNode,
		out XmlElement innerPlotNode,
		out AxisHandlerResult axisHandlerResult)
	{
		// Issue #27: must match the encoding the writer actually uses, or the declaration
		// contradicts the bytes and parsing fails outright.
		var xmlDeclaration = _xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");
		var root = _xmlDocument.DocumentElement;
		_xmlDocument.InsertBefore(xmlDeclaration, root);

		var svg = _xmlDocument.CreateElement(string.Empty, "svg", string.Empty);
		svg.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
		svg.SetAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
		_xmlDocument.AppendChild(svg);
		svg.SetAttribute("width", widthPixels.ToString(CultureInfo.InvariantCulture));
		svg.SetAttribute("height", heightPixels.ToString(CultureInfo.InvariantCulture));

		// Issue #27: a viewBox is required, not optional.
		//
		// Chart.SaveImage produces raster output by rendering to SVG and reloading it through
		// SKSvg, then drawing the resulting picture scaled to the requested pixel size. With
		// width and height but no viewBox there is no user coordinate system to scale from, so
		// the picture bounds fall back to the content bounds and the scale-to-fit blows the
		// drawing up. The visible result was a raster image consisting of one enormously
		// magnified element - a background rectangle - with everything else pushed off canvas.
		//
		// Browsers tolerate the omission because they treat width and height as the viewport,
		// which is why the SVG output looked correct while the PNG did not.
		svg.SetAttribute(
			"viewBox",
			FormattableString.Invariant($"0 0 {widthPixels} {heightPixels}"));

		// Always define a defs node
		defs = _xmlDocument.CreateElement(string.Empty, "defs", string.Empty);
		svg.AppendChild(defs);

		// Chart background area
		chartBackgroundAreaNode = GetGroup(chart.ChartBackgroundArea, "chartBackgroundArea");
		svg.AppendChild(chartBackgroundAreaNode);

		// ChartArea background
		chartAreaNode = GetGroup(chart.ChartArea, "chartArea", chart.ChartBackgroundArea);
		chartBackgroundAreaNode.AppendChild(chartAreaNode);

		// Inner Plot background
		innerPlotNode = GetGroup(chart.ChartArea.InnerPlot, "innerPlot", chart.ChartArea);
		chartAreaNode.AppendChild(innerPlotNode);

		axisHandlerResult = new AxisHandler(chart).Process();
	}

	private void PlotAnnotations(Chart chart, XmlElement chartBackgroundAreaNode)
	{
		// Annotations
		var annotationIndex = 0;
		foreach (var annotation in chart.Annotations)
		{
			var textNode = CreateTextNode(
				$"annotation{annotationIndex++}",
				GetRelativePositionX(chart.ChartBackgroundArea, annotation.GetCanvasXLocationPercent()),
				GetRelativePositionY(chart.ChartBackgroundArea, annotation.GetCanvasYLocationPercent()),
				annotation.Text,
				annotation.HorizontalAlignment,
				annotation.VerticalAlignment,
				new TextStyle(
					annotation.FontWeight,
					annotation.FontFamily,
					annotation.FontSize,
					annotation.StrokeColor,
					annotation.FillColor));
			chartBackgroundAreaNode.AppendChild(textNode);
		}
	}

	/// <summary>
	/// Creates a text node at an absolute position within its enclosing group.
	/// </summary>
	/// <remarks>
	/// Issue #35: the stroke colour is now skipped when transparent. It used to be written
	/// unconditionally, and because <c>ToHex</c> discards alpha a transparent colour became
	/// <c>#000000</c> - a black outline around every label whether or not one was asked for.
	/// The font size is emitted too; it was carried on every element and never used, so all
	/// text rendered at the SVG default size regardless of what was set.
	/// </remarks>
	private XmlElement CreateTextNode(
		string id,
		double x,
		double y,
		string text,
		HorizontalAlignment horizontalAlignment,
		VerticalAlignment verticalAlignment,
		TextStyle style,
		double rotationDegrees = 0)
	{
		var roundedX = Math.Round(x, 2);
		var roundedY = Math.Round(y + BaselineOffset(verticalAlignment, style.FontSize), 2);

		var textNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
		textNode.SetAttribute("x", roundedX.ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("y", roundedY.ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("id", id);
		textNode.InnerText = text;
		textNode.SetAttribute("text-anchor", TextAnchor(horizontalAlignment));

		textNode.SetAttribute("font-weight", style.FontWeight.ToString().ToLowerInvariant());
		textNode.SetAttribute("font-size", style.FontSize.ToString(CultureInfo.InvariantCulture));

		// Issue #60: always name a font, defaulting to the one embedded in this assembly. That
		// does not affect our own raster output, where EmbeddedTypefaceProvider answers every
		// request regardless, but SVG is a public output format and a browser or an editor
		// opening one would otherwise pick its own default and lay the text out differently
		// from the PNG of the same chart. The stack is for those consumers, since Svg.Skia
		// ignores everything after the first entry.
		textNode.SetAttribute(
			"font-family",
			style.FontFamily is { Length: > 0 } ? style.FontFamily : DefaultFontFamilyStack);

		if (style.StrokeColor != Colors.Transparent)
		{
			textNode.SetAttribute("stroke", style.StrokeColor.ToHex());
		}

		textNode.SetAttribute("fill", style.FillColor.ToHex());

		if (Math.Abs(rotationDegrees) > 1e-10)
		{
			textNode.SetAttribute(
				"transform",
				FormattableString.Invariant($"rotate({rotationDegrees} {roundedX} {roundedY})"));
		}

		return textNode;
	}

	/// <summary>
	/// How far below the requested Y the baseline sits, for a vertical alignment.
	/// </summary>
	/// <remarks>
	/// Vertical alignment is resolved here rather than left to the renderer.
	///
	/// alignment-baseline is inconsistently supported: browsers largely ignore it on a bare
	/// text element, and the raster path through Svg.Skia ignores it outright, so every label
	/// fell back to the alphabetic baseline and sat higher than intended. Measured against
	/// DocMagic, X axis labels landed at y 341-348 where the reference put them at 348-359.
	///
	/// Offsetting y by a fraction of the font size instead gives the same result in every
	/// renderer, which is the point: the browser and the PNG have to agree. The fractions are
	/// the usual approximations - an ascent of about four fifths of the em, and a visual
	/// centre about a third of the em above the baseline.
	/// </remarks>
	private static double BaselineOffset(VerticalAlignment verticalAlignment, double fontSize)
		=> verticalAlignment switch
		{
			VerticalAlignment.Top => fontSize * 0.8,
			VerticalAlignment.Middle => fontSize * 0.32,
			VerticalAlignment.Bottom => 0,
			_ => throw new NotSupportedException($"Unsupported VerticalAlignment {verticalAlignment}.")
		};

	/// <summary>
	/// The SVG text-anchor for a horizontal alignment.
	/// </summary>
	private static string TextAnchor(HorizontalAlignment horizontalAlignment)
		=> horizontalAlignment switch
		{
			HorizontalAlignment.Left => "start",
			HorizontalAlignment.Center => "middle",
			HorizontalAlignment.Right => "end",
			_ => throw new NotSupportedException($"Unsupported HorizontalAlignment {horizontalAlignment}.")
		};

	private double GetRelativePositionY(ChartNamedElement chartNamedElement, double yPositionPercent)
		=> heightPixels * (100 - (yPositionPercent * chartNamedElement.GetCanvasHeightPercent() / 100)) / 100;

	private double GetRelativePositionX(ChartNamedElement chartNamedElement, double xPositionPercent)
		=> widthPixels * xPositionPercent * chartNamedElement.GetCanvasWidthPercent() / 100 / 100;

	private XmlElement CreateLine(
		double x1,
		double y1,
		double x2,
		double y2,
		Color color,
		double width,
		ChartDashStyle dashStyle = ChartDashStyle.NotSet)
	{
		var lineNode = _xmlDocument.CreateElement(string.Empty, "line", string.Empty);
		lineNode.SetAttribute("x1", Math.Round(x1, 2).ToString(CultureInfo.InvariantCulture));
		lineNode.SetAttribute("y1", Math.Round(y1, 2).ToString(CultureInfo.InvariantCulture));
		lineNode.SetAttribute("x2", Math.Round(x2, 2).ToString(CultureInfo.InvariantCulture));
		lineNode.SetAttribute("y2", Math.Round(y2, 2).ToString(CultureInfo.InvariantCulture));
		lineNode.SetAttribute("stroke", color.ToHex());
		if (color.A != 255)
		{
			lineNode.SetAttribute(
				"stroke-opacity",
				(color.A / 255f).ToString("F2", CultureInfo.InvariantCulture));
		}

		lineNode.SetAttribute("stroke-width", width.ToString(CultureInfo.InvariantCulture));

		var dashArray = DashArrayFor(dashStyle);
		if (dashArray is not null)
		{
			lineNode.SetAttribute("stroke-dasharray", dashArray);
		}

		return lineNode;
	}

	/// <summary>
	/// The dash pattern for a style, or null for a solid line.
	/// </summary>
	/// <remarks>
	/// The same patterns the series paths use, so an axis line dashed the same way as a series
	/// looks the same.
	/// </remarks>
	private static string? DashArrayFor(ChartDashStyle dashStyle) => dashStyle switch
	{
		ChartDashStyle.Dash => "5,2",
		ChartDashStyle.DashDot => "5,2,1,2",
		ChartDashStyle.DashDotDot => "5,2,1,2,1,2",
		ChartDashStyle.Dot => "1,2",
		_ => null
	};

	/// <summary>
	/// A group node carrying nothing but an id.
	/// </summary>
	private XmlElement CreateGroup(string id)
	{
		var groupNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		groupNode.SetAttribute("id", id);
		return groupNode;
	}

	/// <summary>
	/// A coordinate formatted for a path, to two decimal places and culture-independently.
	/// </summary>
	/// <remarks>
	/// A path built by concatenating interpolated strings is a plain string, so it cannot be
	/// handed to FormattableString.Invariant; formatting each number as it goes is what keeps a
	/// comma-decimal culture from producing an unparseable path.
	/// </remarks>
	private static string N(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

	/// <summary>
	/// A positioned group for an axis, aligned to the plot it annotates.
	/// </summary>
	/// <remarks>
	/// An axis frame is not an independent rectangle: it has to line up with the inner plot along
	/// the dimension they share, or the ticks and labels it draws point at the wrong values. The
	/// axis areas carry their own defaults - 10% in and 90% long - which do not track the plot, so
	/// a caller that moved the plot got axes that stayed put. Measured on a chart with the report
	/// defaults: the value axis line was drawn from y 59 to 360 where the reference render drew it
	/// from 40 to 339, exactly the 5% of the height by which the plot had moved.
	///
	/// The other dimension - how wide the value-axis strip is, how tall the category-axis strip -
	/// stays with the axis area, since that is a real setting.
	/// </remarks>
	private XmlElement GetAxisGroup(Chart chart, AxisArea axis, string id)
	{
		var innerPlot = chart.ChartArea.InnerPlot;
		var isVertical = ReferenceEquals(axis, chart.ChartArea.YAxis)
			|| ReferenceEquals(axis, chart.ChartArea.YAxis2Area);

		if (isVertical)
		{
			axis.YPositionPercent = innerPlot.YPositionPercent;
			axis.HeightPercent = innerPlot.HeightPercent;
		}
		else
		{
			axis.XPositionPercent = innerPlot.XPositionPercent;
			axis.WidthPercent = innerPlot.WidthPercent;
		}

		return GetGroup(axis, id, chart.ChartArea);
	}

	/// <summary>
	/// A positioned group for an element, translated into place.
	/// </summary>
	/// <param name="element">The element the group represents.</param>
	/// <param name="id">The group id.</param>
	/// <param name="within">
	/// The group this one is nested inside, when it is nested inside a positioned one.
	/// </param>
	/// <remarks>
	/// Nesting matters because SVG transforms compound: a group inside a translated group is
	/// already moved by its parent, so translating it by its absolute position moves it twice.
	/// That went unnoticed because every parent sat at the origin in the common case - a chart
	/// area at 0,0 translates to "0,0", which is skipped entirely. Put the legend on the left,
	/// so the chart area starts 20% in, and the plot and its axes were displaced by a further
	/// 20% of the width: the last category fell off the canvas.
	/// </remarks>
	private XmlElement GetGroup(ChartNamedElement element, string id, ChartElement? within = null)
	{
		var groupNode = CreateGroup(id);

		// Y is measured from the bottom here and from the top in SVG, so a position becomes a
		// distance from the top of the element above it.
		var topPercent = 100 - (element.GetCanvasYLocationPercent() + element.GetCanvasHeightPercent());
		var leftPercent = element.GetCanvasXLocationPercent();

		if (within is not null)
		{
			leftPercent -= within.GetCanvasXLocationPercent();
			topPercent -= 100 - (within.GetCanvasYLocationPercent() + within.GetCanvasHeightPercent());
		}

		var translation = $"{widthPixels * leftPercent / 100},{heightPixels * topPercent / 100}";
		if (translation != "0,0")
		{
			groupNode.SetAttribute("transform", $"translate({translation})");
		}

		var rectNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
		rectNode.SetAttribute("width", (widthPixels * element.GetCanvasWidthPercent() / 100).ToString(CultureInfo.InvariantCulture));
		rectNode.SetAttribute("height", (heightPixels * element.GetCanvasHeightPercent() / 100).ToString(CultureInfo.InvariantCulture));
		if (element.XRadiusPixels != 0)
		{
			rectNode.SetAttribute("rx", element.XRadiusPixels.ToString(CultureInfo.InvariantCulture));
		}

		if (element.YRadiusPixels != 0)
		{
			rectNode.SetAttribute("ry", element.YRadiusPixels.ToString(CultureInfo.InvariantCulture));
		}

		rectNode.SetStyle(element);
		groupNode.AppendChild(rectNode);

		if (debug)
		{
			var debugTextNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
			debugTextNode.SetAttribute("alignment-baseline", "hanging");
			debugTextNode.InnerText = element.Name;
			groupNode.AppendChild(debugTextNode);
		}

		return groupNode;
	}
}
