using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers;

internal class InternalSvgRenderer(int widthPixels, int heightPixels, bool debug)
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

	/// <summary>
	/// The offset that puts a pie start angle of zero at three o clock.
	/// </summary>
	private const double QuarterTurnDegrees = 90;

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
				annotation.FontWeight,
				annotation.FontFamily,
				annotation.FontSize,
				annotation.StrokeColor,
				annotation.FillColor);
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
		FontWeight fontWeight,
		string? fontFamily,
		double fontSize,
		Color strokeColor,
		Color fillColor,
		double rotationDegrees = 0)
	{
		// Vertical alignment is resolved here rather than left to the renderer.
		//
		// alignment-baseline is inconsistently supported: browsers largely ignore it on a bare
		// text element, and the raster path through Svg.Skia ignores it outright, so every label
		// fell back to the alphabetic baseline and sat higher than intended. Measured against
		// DocMagic, X axis labels landed at y 341-348 where the reference put them at 348-359.
		//
		// Offsetting y by a fraction of the font size instead gives the same result in every
		// renderer, which is the point: the browser and the PNG have to agree. The fractions are
		// the usual approximations - an ascent of about four fifths of the em, and a visual
		// centre about a third of the em above the baseline.
		var baselineOffset = verticalAlignment switch
		{
			VerticalAlignment.Top => fontSize * 0.8,
			VerticalAlignment.Middle => fontSize * 0.32,
			VerticalAlignment.Bottom => 0,
			_ => throw new NotSupportedException($"Unsupported VerticalAlignment {verticalAlignment}.")
		};

		var roundedX = Math.Round(x, 2);
		var roundedY = Math.Round(y + baselineOffset, 2);

		var textNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
		textNode.SetAttribute("x", roundedX.ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("y", roundedY.ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("id", id);
		textNode.InnerText = text;
		textNode.SetAttribute("text-anchor", horizontalAlignment switch
		{
			HorizontalAlignment.Left => "start",
			HorizontalAlignment.Center => "middle",
			HorizontalAlignment.Right => "end",
			_ => throw new NotSupportedException($"Unsupported HorizontalAlignment {horizontalAlignment}.")
		});

		textNode.SetAttribute("font-weight", fontWeight.ToString().ToLowerInvariant());
		textNode.SetAttribute("font-size", fontSize.ToString(CultureInfo.InvariantCulture));
		// Issue #60: always name a font, defaulting to the one embedded in this assembly.
		//
		// This does not affect our own raster output - EmbeddedTypefaceProvider answers every
		// request there regardless - but SVG is a public output format, and a browser or an
		// editor opening one would otherwise pick its own default and lay the text out
		// differently from the PNG of the same chart. The stack is for those consumers;
		// Svg.Skia ignores everything after the first entry.
		textNode.SetAttribute(
			"font-family",
			fontFamily is { Length: > 0 } ? fontFamily : DefaultFontFamilyStack);

		if (strokeColor != Colors.Transparent)
		{
			textNode.SetAttribute("stroke", strokeColor.ToHex());
		}

		textNode.SetAttribute("fill", fillColor.ToHex());

		if (rotationDegrees != 0)
		{
			textNode.SetAttribute(
				"transform",
				FormattableString.Invariant($"rotate({rotationDegrees} {roundedX} {roundedY})"));
		}

		return textNode;
	}

	private double GetRelativePositionY(ChartNamedElement chartNamedElement, double yPositionPercent)
		=> heightPixels * (100 - (yPositionPercent * chartNamedElement.GetCanvasHeightPercent() / 100)) / 100;

	private double GetRelativePositionX(ChartNamedElement chartNamedElement, double xPositionPercent)
		=> widthPixels * xPositionPercent * chartNamedElement.GetCanvasWidthPercent() / 100 / 100;

	/// <summary>
	/// Draws the legend: one swatch and label per series, inside the legend box.
	/// </summary>
	/// <remarks>
	/// Issue #35: laid out in the legend's own pixel space. The previous version worked in
	/// percentages and then passed them through a helper that scaled by the legend width a
	/// second time, collapsing the spacing to a fraction of what was intended - which is why
	/// three labels landed almost on top of one another. Swatch sizes were percentages of the
	/// whole image rather than of the legend, so they drifted with the output size.
	/// </remarks>
	private void PlotLegends(Chart chart, XmlElement chartBackgroundAreaNode)
	{
		if (chart.Legends.Count == 0 || chart.Series.Count == 0)
		{
			return;
		}

		var legend = chart.Legends[0];
		var legendXmlElement = GetGroup(legend, "legend", chart.ChartBackgroundArea);
		chartBackgroundAreaNode.AppendChild(legendXmlElement);

		var legendWidth = widthPixels * legend.GetCanvasWidthPercent() / 100;
		var legendHeight = heightPixels * legend.GetCanvasHeightPercent() / 100;
		var fontSize = legend.FontSize;
		var swatchWidth = SwatchWidth(fontSize);
		var swatchSize = SwatchHeight(fontSize);
		var padding = Math.Round(fontSize * 0.5, 2);

		var seriesIndex = 0;
		foreach (var series in chart.Series)
		{
			double swatchX;
			double swatchY;

			switch (legend.Style)
			{
				case LegendStyle.Row:
					{
						// Entries packed one after another and the row centred, rather than each given an
						// equal share of the width.
						//
						// Spreading them looks tidy on paper and is wrong twice over. It does not match the
						// reference render, which packs them and centres the result; and once the swatch
						// became a rectangle rather than a small square, a slot sized without reference to
						// its contents put the next swatch on top of the previous label.
						var entryWidths = chart.Series
							.Select(series => swatchWidth + (padding / 2) + EstimateTextWidth(LegendTextFor(series), fontSize))
							.ToList();

						var gap = padding * 2;
						var rowWidth = entryWidths.Sum() + (gap * (entryWidths.Count - 1));

						swatchX = Math.Round(
							Math.Max(padding, (legendWidth - rowWidth) / 2)
								+ entryWidths.Take(seriesIndex).Sum()
								+ (gap * seriesIndex),
							2);
						swatchY = Math.Round((legendHeight - swatchSize) / 2, 2);
						break;
					}

				case LegendStyle.Column:
					{
						// One row per series, spread down the legend rather than packed together, and
						// left-aligned at an inset proportional to the legend width.
						//
						// Both measured against the renderer this matches, which gives each entry an equal
						// share of the legend height: on a 400-pixel legend it spaced three entries 129
						// apart and two 193 apart, which is the height less one swatch, divided by the
						// count. Packing them at 1.6 line heights put all three within 50 pixels of the
						// middle and left most of the legend empty.
						swatchX = Math.Round(legendWidth * LegendInsetFraction, 2);
						swatchY = Math.Round(
							RowCentre(legendHeight, seriesIndex, chart.Series.Count, swatchSize)
								- (swatchSize / 2),
							2);
						break;
					}

				default:
					throw new NotSupportedException($"Legend style {legend.Style} is not supported.");
			}

			// A line series is represented by a bar rather than a block, so that the legend
			// distinguishes a line from a filled area at a glance.
			var isLine = series.ChartType
				is SeriesChartType.Line
				or SeriesChartType.FastLine
				or SeriesChartType.Spline
				or SeriesChartType.StepLine;
			var swatchHeight = isLine ? Math.Max(2, Math.Round(swatchSize / 4, 2)) : swatchSize;
			var swatchTop = isLine ? swatchY + ((swatchSize - swatchHeight) / 2) : swatchY;

			// A line series carries its identity in its stroke, a filled series in its fill.
			var swatchColor = isLine
				? series.StrokeColor
				: series.FillColor != Colors.Transparent ? series.FillColor : series.StrokeColor;

			var swatchNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
			swatchNode.SetAttribute("x", swatchX.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("y", swatchTop.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("width", swatchWidth.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("height", swatchHeight.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("fill", swatchColor.ToHex());
			if (swatchColor.A != 255)
			{
				swatchNode.SetAttribute(
					"fill-opacity",
					(swatchColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture));
			}

			legendXmlElement.AppendChild(swatchNode);

			legendXmlElement.AppendChild(
				CreateTextNode(
					$"legendSeries{seriesIndex}Text",
					swatchX + swatchWidth + (padding / 2),
					swatchY + (swatchSize / 2),
					series.LegendText is { Length: > 0 } ? series.LegendText : series.Name,
					HorizontalAlignment.Left,
					VerticalAlignment.Middle,
					legend.FontWeight,
					legend.FontFamily,
					fontSize,
					Colors.Transparent,
					legend.FontColor));

			seriesIndex++;
		}
	}

	/// <summary>
	/// Draws gridlines across the plot for whichever axes asked for them.
	/// </summary>
	/// <remarks>
	/// Issue #31: gridlines belong to the axis whose values they mark, so the Y axis draws
	/// horizontal lines and the X axis vertical ones.
	/// </remarks>
	private void PlotGridlines(Chart chart, PlotGeometry geometry, XmlElement innerPlotNode)
	{
		var xAxis = chart.ChartArea.XAxis;
		var yAxis = chart.ChartArea.YAxis;

		if (!yAxis.MajorGridEnabled && !yAxis.MinorGridEnabled && !xAxis.MajorGridEnabled && !xAxis.MinorGridEnabled)
		{
			return;
		}

		var gridNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		gridNode.SetAttribute("id", "gridlines");
		innerPlotNode.AppendChild(gridNode);

		// Minor lines before major, so a major line is drawn over a coincident minor one.
		if (yAxis.MinorGridEnabled)
		{
			foreach (var value in MinorTicks(yAxis, geometry, isValueAxis: !geometry.IsHorizontalPlot))
			{
				var y = geometry.YToPixels(value);
				gridNode.AppendChild(CreateLine(0, y, geometry.Width, y, yAxis.MinorGridColor, yAxis.GridWidth));
			}
		}

		if (yAxis.MajorGridEnabled)
		{
			foreach (var value in YAxisTickValues(chart, geometry))
			{
				var y = geometry.IsHorizontalPlot ? geometry.CategoryToPixels(value) : geometry.YToPixels(value);
				gridNode.AppendChild(CreateLine(0, y, geometry.Width, y, yAxis.MajorGridColor, yAxis.GridWidth));
			}
		}

		if (xAxis.MinorGridEnabled)
		{
			foreach (var x in MinorGridPositions(xAxis, geometry))
			{
				gridNode.AppendChild(CreateLine(x, 0, x, geometry.Height, xAxis.MinorGridColor, xAxis.GridWidth));
			}
		}

		if (xAxis.MajorGridEnabled)
		{
			foreach (var value in XAxisTickValues(chart, geometry))
			{
				var x = XAxisPixels(geometry, value);
				gridNode.AppendChild(CreateLine(x, 0, x, geometry.Height, xAxis.MajorGridColor, xAxis.GridWidth));
			}
		}
	}

	/// <summary>
	/// Draws the axis strips: their backgrounds, then the axis line, ticks, labels and title.
	/// </summary>
	private void PlotAxes(Chart chart, PlotGeometry geometry, XmlElement chartAreaNode)
	{
		// X Axis
		var xAxis = chart.ChartArea.XAxis;
		var xAxisNode = GetAxisGroup(chart, xAxis, "xAxis");
		chartAreaNode.AppendChild(xAxisNode);
		if (xAxis.IsEnabled && xAxis.LabelsEnabled)
		{
			DrawXAxis(chart, geometry, xAxis, xAxisNode);
		}

		// Y Axis
		var yAxis = chart.ChartArea.YAxis;
		var yAxisNode = GetAxisGroup(chart, yAxis, "yAxis");
		chartAreaNode.AppendChild(yAxisNode);
		if (yAxis.IsEnabled && yAxis.LabelsEnabled)
		{
			DrawYAxis(chart, geometry, yAxis, yAxisNode);
		}
	}

	/// <summary>
	/// The X axis strip sits directly beneath the plot and shares its width and horizontal
	/// origin, so a local X coordinate in one is the same local X coordinate in the other, and
	/// the strip top edge is the plot bottom edge.
	/// </summary>
	private void DrawXAxis(Chart chart, PlotGeometry geometry, AxisArea xAxis, XmlElement xAxisNode)
	{
		var axisHeight = heightPixels * xAxis.GetCanvasHeightPercent() / 100;

		xAxisNode.AppendChild(CreateLine(0, 0, geometry.Width, 0, xAxis.LineColor, xAxis.LineWidth, xAxis.LineDashStyle));

		var tickLength = xAxis.TickLengthPixels;
		var labelY = tickLength + TickLabelGapPixels;
		var isRotated = xAxis.LabelAngle != 0;

		foreach (var value in XAxisTickValues(chart, geometry))
		{
			var x = XAxisPixels(geometry, value);
			xAxisNode.AppendChild(CreateLine(x, 0, x, tickLength, xAxis.LineColor, xAxis.LineWidth));

			var label = geometry.IsHorizontalPlot
				? FormatAxisValue(value, xAxis)
				: geometry.CategoryLabel(value) ?? FormatAxisValue(value, xAxis);

			xAxisNode.AppendChild(
				CreateTextNode(
					FormattableString.Invariant($"xAxisLabel{x}"),
					x,
					labelY,
					label,
					// A rotated label reads better anchored at its end, so that it runs away
					// from its tick rather than across it.
					isRotated ? HorizontalAlignment.Right : HorizontalAlignment.Center,
					VerticalAlignment.Top,
					xAxis.FontWeight,
					xAxis.FontFamily,
					xAxis.FontSize,
					Colors.Transparent,
					xAxis.FontColor,
					xAxis.LabelAngle));
		}

		if (xAxis.Title is { Length: > 0 })
		{
			xAxisNode.AppendChild(
				CreateTextNode(
					"xAxisTitle",
					geometry.Width / 2,
					// Held clear of the bottom edge: on the baseline exactly, the descenders fall
					// outside the viewport and are clipped.
					axisHeight - (xAxis.FontSize * 0.2),
					xAxis.Title,
					HorizontalAlignment.Center,
					VerticalAlignment.Bottom,
					FontWeight.Bold,
					xAxis.FontFamily,
					xAxis.FontSize,
					Colors.Transparent,
					xAxis.FontColor));
		}
	}

	/// <summary>
	/// The Y axis strip sits immediately left of the plot and shares its height, so the axis
	/// line is drawn along the strip right-hand edge, which is the plot left edge.
	/// </summary>
	private void DrawYAxis(Chart chart, PlotGeometry geometry, AxisArea yAxis, XmlElement yAxisNode)
	{
		var axisWidth = widthPixels * yAxis.GetCanvasWidthPercent() / 100;

		yAxisNode.AppendChild(CreateLine(axisWidth, 0, axisWidth, geometry.Height, yAxis.LineColor, yAxis.LineWidth, yAxis.LineDashStyle));

		var tickLength = yAxis.TickLengthPixels;
		var labelX = axisWidth - tickLength - TickLabelGapPixels;

		foreach (var value in YAxisTickValues(chart, geometry))
		{
			var y = geometry.IsHorizontalPlot ? geometry.CategoryToPixels(value) : geometry.YToPixels(value);
			yAxisNode.AppendChild(
				CreateLine(axisWidth - tickLength, y, axisWidth, y, yAxis.LineColor, yAxis.LineWidth));

			var label = geometry.IsHorizontalPlot
				? geometry.CategoryLabel(value) ?? FormatAxisValue(value, yAxis)
				: FormatAxisValue(value, yAxis);

			yAxisNode.AppendChild(
				CreateTextNode(
					FormattableString.Invariant($"yAxisLabel{y}"),
					labelX,
					y,
					label,
					HorizontalAlignment.Right,
					VerticalAlignment.Middle,
					yAxis.FontWeight,
					yAxis.FontFamily,
					yAxis.FontSize,
					Colors.Transparent,
					yAxis.FontColor,
					yAxis.LabelAngle));
		}

		if (yAxis.Title is { Length: > 0 })
		{
			// Rotated a quarter turn anticlockwise and centred on the axis, as a Y axis title
			// conventionally reads.
			yAxisNode.AppendChild(
				CreateTextNode(
					"yAxisTitle",
					yAxis.FontSize * 0.9,
					geometry.Height / 2,
					yAxis.Title,
					HorizontalAlignment.Center,
					VerticalAlignment.Top,
					FontWeight.Bold,
					yAxis.FontFamily,
					yAxis.FontSize,
					Colors.Transparent,
					yAxis.FontColor,
					-90));
		}
	}

	private static double XAxisPixels(PlotGeometry geometry, double value)
		=> geometry.IsHorizontalPlot
			? geometry.ValueToPixels(value)
			: geometry.IsCategorical ? geometry.CategoryToPixels(value) : geometry.XToPixels(value);

	/// <summary>
	/// The values the X axis is labelled at: one per category when the axis is categorical, the
	/// value scale for a bar chart, and readable intervals across the range otherwise.
	/// </summary>
	private static IReadOnlyList<double> XAxisTickValues(Chart chart, PlotGeometry geometry)
	{
		if (geometry.IsHorizontalPlot)
		{
			return TickGenerator.Linear(
				geometry.YDisplayStart,
				geometry.YDisplayEnd,
				chart.ChartArea.XAxis.Interval,
				chart.ChartArea.XAxis.TargetTickCount);
		}

		if (geometry.IsCategorical)
		{
			// An interval on a category axis means every Nth category, which is how a long set of
			// labels is thinned. Ignoring it left every category labelled however many there were.
			var step = (int)Math.Round(chart.ChartArea.XAxis.Interval ?? 1);
			return step <= 1
				? geometry.Categories
				: [.. geometry.Categories.Where((_, index) => index % step == 0)];
		}

		return geometry.IsCategorical
			? geometry.Categories
			: TickGenerator.Linear(
				geometry.XDisplayStart,
				geometry.XDisplayEnd,
				chart.ChartArea.XAxis.Interval,
				chart.ChartArea.XAxis.TargetTickCount);
	}

	/// <summary>
	/// The values the Y axis is labelled at.
	/// </summary>
	private static IReadOnlyList<double> YAxisTickValues(Chart chart, PlotGeometry geometry)
	{
		if (geometry.IsHorizontalPlot)
		{
			return geometry.Categories;
		}

		return geometry.YIsLogarithmic
			? TickGenerator.Logarithmic(geometry.YLogMinimum, geometry.YLogMaximum, includeMinor: false)
			: TickGenerator.Linear(
				geometry.YDisplayStart,
				geometry.YDisplayEnd,
				// The interval the bounds were derived from, so the labels land on the bounds
				// rather than being chosen again from the adjusted range.
				chart.ChartArea.YAxis.Interval ?? geometry.ValueAxisInterval,
				chart.ChartArea.YAxis.TargetTickCount);
	}

	/// <summary>
	/// Where the vertical minor gridlines go, in pixels across the plot.
	/// </summary>
	/// <remarks>
	/// A category axis subdivides its bands. This used to draw nothing at all, on the reasoning
	/// that there is nothing between one category and the next - but the reference draws them,
	/// roughly four to a band, so a chart asking for X minor gridlines got none and the setting
	/// was neither honoured nor refused.
	/// </remarks>
	private static IReadOnlyList<double> MinorGridPositions(AxisArea axis, PlotGeometry geometry)
	{
		if (geometry.IsHorizontalPlot)
		{
			return [.. MinorTicks(axis, geometry, isValueAxis: true).Select(v => XAxisPixels(geometry, v))];
		}

		if (!geometry.IsCategorical)
		{
			var span = geometry.XDisplayEnd - geometry.XDisplayStart;
			var interval = axis.MinorGridInterval is > 0
				? axis.MinorGridInterval.Value
				: span / Math.Max(axis.TargetTickCount, 1) / Math.Max(axis.MinorGridSubdivisions, 1);

			return
			[
				.. TickGenerator
					.Linear(geometry.XDisplayStart, geometry.XDisplayEnd, interval, axis.TargetTickCount * axis.MinorGridSubdivisions)
					.Select(geometry.XToPixels)
			];
		}

		// Subdivisions of each category band, measured from the band edge rather than its
		// centre, so the lines fall between the categories as well as within them.
		var subdivisions = Math.Max(axis.MinorGridSubdivisions, 1);
		var band = geometry.CategoryBandExtent;
		if (band <= 0)
		{
			return [];
		}

		var step = band / subdivisions;
		var positions = new List<double>();
		for (var x = 0d; x <= geometry.Width + 0.001; x += step)
		{
			positions.Add(Math.Round(x, 2));
			if (positions.Count > 2000)
			{
				break;
			}
		}

		return positions;
	}

	/// <summary>
	/// Minor gridline positions: the interval the caller gave, otherwise a subdivision of the
	/// major interval, or the intermediate steps within each decade on a logarithmic axis.
	/// </summary>
	private static IReadOnlyList<double> MinorTicks(AxisArea axis, PlotGeometry geometry, bool isValueAxis)
	{
		if (!isValueAxis)
		{
			// Subdivisions of the category band. This used to return nothing, on the reasoning
			// that there is nothing between one category and the next - but the reference draws
			// them, about four to a band, so a chart asking for X minor gridlines got none.
			return [];
		}

		if (geometry.YIsLogarithmic)
		{
			return TickGenerator.Logarithmic(geometry.YLogMinimum, geometry.YLogMaximum, includeMinor: true);
		}

		var interval = axis.MinorGridInterval;
		if (interval is not > 0)
		{
			// Five subdivisions of the target major spacing, which is a conventional
			// minor-to-major ratio and keeps the count bounded.
			interval = (geometry.YDisplayEnd - geometry.YDisplayStart) / Math.Max(axis.TargetTickCount, 1) / 5;
		}

		return TickGenerator.Linear(
			geometry.YDisplayStart,
			geometry.YDisplayEnd,
			interval,
			axis.TargetTickCount * 5);
	}

	/// <summary>
	/// Formats an axis value, honouring an explicit format string and the short-label option.
	/// </summary>
	private static string FormatAxisValue(double value, AxisArea axis)
	{
		if (axis.LabelFormat is { Length: > 0 })
		{
			return value.ToString(axis.LabelFormat, CultureInfo.InvariantCulture);
		}

		if (axis.UseShortLabels)
		{
			// One decimal place, with a suffix once the value reaches a thousand.
			//
			// Measured against DocMagic: with short labels on and values topping out at 35, its
			// axis reads 35.0, 30.0, 25.0 rather than 35, 30, 25. So "short" is not only about
			// abbreviating large numbers - it is a fixed one-decimal format throughout, and this
			// implementation left anything under a thousand alone, which is why the setting had no
			// effect on a percentage axis.
			var absolute = Math.Abs(value);
			if (absolute >= 1_000_000_000)
			{
				return FormattableString.Invariant($"{value / 1_000_000_000:0.0}G");
			}

			if (absolute >= 1_000_000)
			{
				return FormattableString.Invariant($"{value / 1_000_000:0.0}M");
			}

			if (absolute >= 1_000)
			{
				return FormattableString.Invariant($"{value / 1_000:0.0}K");
			}

			return value.ToString("0.0", CultureInfo.InvariantCulture);
		}
		// Two decimal places at most, and none where the value does not need them.
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

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

	private void PlotSeries(Chart chart, PlotGeometry geometry, XmlElement defs, XmlElement innerPlotNode)
	{
		var innerPlotHeight = geometry.Height;
		var innerPlotWidth = geometry.Width;
		var stackedColumnDictionary = new Dictionary<string, double>();
		var stackedAreaDictionary = new Dictionary<string, double>();
		var stackLines = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		stackLines.SetAttribute("id", "stackLines");

		// Issue #33: a column or bar occupies a slot within its category band. Grouped series
		// take one slot each; all stacked series share a single slot, because they stack on top
		// of one another rather than standing side by side.
		var bandedSeries = chart.Series.Where(s => PlotGeometry.IsBanded(s.ChartType)).ToList();
		var groupedSeries = bandedSeries.Where(s => !PlotGeometry.IsStacked(s.ChartType)).ToList();
		var hasStackedBanded = bandedSeries.Exists(s => PlotGeometry.IsStacked(s.ChartType));
		var slotCount = Math.Max(1, groupedSeries.Count + (hasStackedBanded ? 1 : 0));

		var seriesIndex = -1;
		foreach (var series in chart.Series)
		{
			var seriesNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
			seriesNode.SetAttribute("id", $"series{++seriesIndex}");

			// Add markers to defs if required
			var seriesMarkerId = $"series{seriesIndex}Marker";
			var markerDefinition = CreateMarkerDefinition(series, seriesMarkerId);
			if (markerDefinition is not null)
			{
				defs.AppendChild(markerDefinition);
			}
			var stackDictionary = series.ChartType switch
			{
				SeriesChartType.StackedColumn or SeriesChartType.StackedColumn100 => stackedColumnDictionary,
				SeriesChartType.StackedBar or SeriesChartType.StackedBar100 => stackedColumnDictionary,
				SeriesChartType.StackedArea or SeriesChartType.StackedArea100 => stackedAreaDictionary,
				_ => null
			};

			if (PlotGeometry.IsBanded(series.ChartType))
			{
				var slot = PlotGeometry.IsStacked(series.ChartType)
					? groupedSeries.Count
					: groupedSeries.IndexOf(series);

				PlotBandedSeries(chart, geometry, series, seriesNode, stackDictionary, slot, slotCount);
				innerPlotNode.AppendChild(seriesNode);
				continue;
			}

			var pathNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
			var areaNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
			var pathStringBuilder = new StringBuilder();
			// The outline only. Where the fill starts and finishes is decided once the first and last
			// points are known, because it belongs under them and not at the edges of the plot.
			var areaStringBuilder = new StringBuilder();
			double? firstXPosition = null;
			var lastXPosition = 0d;
			var returnPathPoints = new List<Tuple<double, double>>();
			var isFirstPoint = true;
			var markerNodes = new List<XmlElement>();
			foreach (var chartPoint in series.Points)
			{
				var xValue = chartPoint.XValue;
				var xValueString = xValue.ToString(CultureInfo.InvariantCulture);
				var yPointValue = chartPoint.YValue;
				double yValue;
				var previousYValue = stackDictionary is not null ? stackDictionary.TryGetValue(xValueString, out var stackedColumnValue) ? (double?)stackedColumnValue : null : null;
				if (stackDictionary is not null && yPointValue is not null)
				{
					// As above: a share of the category rather than the value itself.
					var contribution = geometry.IsPercentStackedPlot
						? geometry.ToPercentOfCategory(chartPoint.XValue, yPointValue.Value)
						: yPointValue.Value;

					yValue = contribution + (previousYValue ?? 0);
					stackDictionary[xValueString] = yValue;
				}
				else
				{
					yValue = yPointValue ?? 0;
				}

				var xPosition = geometry.IsCategorical
					? geometry.CategoryToPixels(xValue)
					: geometry.XToPixels(xValue);
				var yPosition = geometry.YToPixels(yValue);
				if (previousYValue is not null)
				{
					returnPathPoints.Add(new Tuple<double, double>(xPosition, geometry.YToPixels((double)previousYValue)));
				}

				// Letter - always M to start, afterwards L unless the previous value is null
				pathStringBuilder.Append($"{(isFirstPoint ? "M" : " L")}{xPosition} {yPosition}");
				areaStringBuilder.Append($" L{xPosition} {yPosition}");
				firstXPosition ??= xPosition;
				lastXPosition = xPosition;
				isFirstPoint = false;

				// Add marker if appropriate
				if (series.MarkerStyle != MarkerStyle.None)
				{
					var markerNode = _xmlDocument.CreateElement(string.Empty, "use", string.Empty);
					markerNode.SetAttribute("xlink:href", $"#{seriesMarkerId}");
					markerNode.SetAttribute("transform", $"translate({xPosition} {yPosition})");
					markerNodes.Add(markerNode);
				}
			}

			// Fill Area
			switch (series.ChartType)
			{
				case SeriesChartType.Area:
				case SeriesChartType.StackedArea:
				case SeriesChartType.StackedArea100:
					// The fill hangs below the line it follows, from the first point to the last. It used to
					// start at the bottom-left corner of the plot and finish at the bottom-right, which drew
					// a diagonal ramp up to the first point and another down from the last - inventing data
					// on either side of the series. With a whole category interval of padding at each end of
					// the axis, those ramps were a sixth of the chart wide.
					//
					// And it hangs to the zero line, not to the floor of the plot, so a series with negative
					// values fills downwards from zero rather than upwards from the bottom.
					var baseline = geometry.ValueAxisOrigin;

					if (returnPathPoints.Count == 0)
					{
						returnPathPoints.Add(new(lastXPosition, baseline));
					}

					var areaPath = new StringBuilder(
						FormattableString.Invariant($"M{firstXPosition ?? 0} {baseline}"));
					areaPath.Append(areaStringBuilder);
					areaPath.Append(string.Join("", returnPathPoints.AsEnumerable().Reverse().Select(p => $"L{p.Item1} {p.Item2}")));
					areaPath.Append('Z');
					areaNode.SetAttribute("d", areaPath.ToString());
					areaNode.SetStyle(series, applyStroke: false);
					seriesNode.AppendChild(areaNode);

					break;
			}

			// Line
			switch (series.ChartType)
			{
				case SeriesChartType.Area:
				case SeriesChartType.Line:
				case SeriesChartType.FastLine:
					pathNode.SetAttribute("d", pathStringBuilder.ToString());
					pathNode.SetStyle(series, applyFill: false);
					seriesNode.AppendChild(pathNode);
					// Markers
					foreach (var markerNode in markerNodes)
					{
						seriesNode.AppendChild(markerNode);
					}

					break;
				case SeriesChartType.StackedArea:
				case SeriesChartType.StackedArea100:
					pathNode.SetAttribute("d", pathStringBuilder.ToString());
					pathNode.SetStyle(series, applyFill: false);
					stackLines.AppendChild(pathNode);
					// Markers
					foreach (var markerNode in markerNodes)
					{
						stackLines.AppendChild(markerNode);
					}

					break;
			}

			innerPlotNode.AppendChild(seriesNode);
		}

		if (stackLines.ChildNodes.Count != 0)
		{
			innerPlotNode.AppendChild(stackLines);
		}
	}

	/// <summary>
	/// Draws one column or bar series: a rectangle per point, running from the value axis origin
	/// to the value of the point, occupying its slot within the category band.
	/// </summary>
	/// <remarks>
	/// Issue #33: <c>InternalSvgRenderer</c> had no case for any of these chart types, so a
	/// column chart rendered its legend and nothing else - no exception, no empty-plot warning,
	/// just a blank plot area beside a correct-looking legend.
	/// </remarks>
	private void PlotBandedSeries(
		Chart chart,
		PlotGeometry geometry,
		Series series,
		XmlElement seriesNode,
		Dictionary<string, double>? stackDictionary,
		int slot,
		int slotCount)
	{
		var bandExtent = geometry.CategoryBandExtent;
		var groupExtent = bandExtent * chart.ChartArea.ColumnBandFillFraction;
		var slotExtent = groupExtent / slotCount;
		var origin = geometry.ValueAxisOrigin;
		var isHorizontal = PlotGeometry.IsHorizontal(series.ChartType);

		foreach (var chartPoint in series.Points)
		{
			if (chartPoint.YValue is null)
			{
				continue;
			}

			var key = chartPoint.XValue.ToString(CultureInfo.InvariantCulture);
			double from;
			double to;
			if (stackDictionary is not null)
			{
				var previousTotal = stackDictionary.TryGetValue(key, out var runningTotal) ? runningTotal : 0;

				// A hundred per cent stacked series contributes its share of the category, not its value.
				var contribution = geometry.IsPercentStackedPlot
					? geometry.ToPercentOfCategory(chartPoint.XValue, chartPoint.YValue.Value)
					: chartPoint.YValue.Value;

				var newTotal = previousTotal + contribution;
				stackDictionary[key] = newTotal;
				from = geometry.ValueToPixels(previousTotal);
				to = geometry.ValueToPixels(newTotal);
			}
			else
			{
				from = origin;
				to = geometry.ValueToPixels(chartPoint.YValue.Value);
			}

			var slotStart = geometry.CategoryToPixels(chartPoint.XValue) - (groupExtent / 2) + (slot * slotExtent);

			var rectNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
			var near = Math.Round(Math.Min(from, to), 2).ToString(CultureInfo.InvariantCulture);
			var extent = Math.Round(Math.Abs(to - from), 2).ToString(CultureInfo.InvariantCulture);
			var across = Math.Round(slotStart, 2).ToString(CultureInfo.InvariantCulture);
			var thickness = Math.Round(slotExtent, 2).ToString(CultureInfo.InvariantCulture);

			if (isHorizontal)
			{
				rectNode.SetAttribute("x", near);
				rectNode.SetAttribute("y", across);
				rectNode.SetAttribute("width", extent);
				rectNode.SetAttribute("height", thickness);
			}
			else
			{
				rectNode.SetAttribute("x", across);
				rectNode.SetAttribute("y", near);
				rectNode.SetAttribute("width", thickness);
				rectNode.SetAttribute("height", extent);
			}

			rectNode.SetStyle(series);
			seriesNode.AppendChild(rectNode);
		}
	}

	/// <summary>
	/// Whether this chart type is drawn as a ring of slices rather than against axes.
	/// </summary>
	private static bool IsPie(Series series)
		=> series.ChartType is SeriesChartType.Pie or SeriesChartType.Doughnut;

	/// <summary>
	/// Draws a pie or doughnut: one wedge per slice, centred in the chart area, with the slice
	/// labels inside, outside on a leader line, or not at all.
	/// </summary>
	/// <remarks>
	/// Angles run clockwise from twelve o'clock, as they do in the Microsoft chart control, so a
	/// start angle of zero puts the first slice boundary at the top.
	/// </remarks>
	private void PlotPie(Series series, List<PieSlice> slices, XmlElement innerPlotNode, double plotWidth, double plotHeight)
	{
		if (slices.Count == 0)
		{
			return;
		}

		var pieNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		pieNode.SetAttribute("id", "pie");
		innerPlotNode.AppendChild(pieNode);

		// Centred in the inner plot, not the chart area. Measured against DocMagic: for a chart
		// area 468x400 with an inner plot inset 10% left and 10% vertically, its pie centre was
		// the inner plot centre and its diameter exactly 0.95 of the shorter inner plot side.
		// Drawing in the chart area instead put the pie 24px left and 20px high, and 8% small.
		var centreX = plotWidth / 2;
		var centreY = plotHeight / 2;

		// Labels drawn outside need room for themselves, so the pie is drawn smaller.
		var labelsOutside = series.PieLabelStyle == PieLabelStyle.Outside;

		// The pie is the same size whether its labels are inside or out. Shrinking it to make room
		// for outside labels seems the considerate thing to do, but the renderer this matches does
		// not: measured at 283 pixels across with outside labels and 285 with inside ones, where
		// shrinking gave 225. The labels are allowed to overflow instead, which is what the
		// reference render does - one of them sits outside the chart area entirely.
		var radius = Math.Min(plotWidth, plotHeight) / 2 * 0.95;

		// The percentage is the width of the RING, not the size of the hole, so the hole is what is
		// left over. This was the other way round, which inverted every doughnut: the default of 60
		// drew a 60% hole where the Microsoft chart control draws a 40% one, and asking for a thin
		// 30% ring gave a fat one.
		//
		// Measured on two doughnuts against the reference render, at 285 pixels across: the default
		// left a hole 116 wide (40.7%) and a specified 30 left one 202 wide (70.9%). Both are
		// 100 minus the percentage, and neither is the percentage itself.
		var innerRadius = series.ChartType == SeriesChartType.Doughnut
			? radius * (100 - Math.Clamp(series.DoughnutRadiusPercent ?? 60, 1, 100)) / 100
			: 0;

		foreach (var slice in slices)
		{
			var wedge = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
			wedge.SetAttribute("d", WedgePath(centreX, centreY, radius, innerRadius, slice));
			wedge.SetAttribute("fill", slice.Color.ToHex());
			if (slice.Color.A != 255)
			{
				wedge.SetAttribute(
					"fill-opacity",
					(slice.Color.A / 255f).ToString("F2", CultureInfo.InvariantCulture));
			}

			if (series.StrokeColor != Colors.Transparent && series.StrokeWidth > 0)
			{
				wedge.SetAttribute("stroke", series.StrokeColor.ToHex());
				wedge.SetAttribute("stroke-width", series.StrokeWidth.ToString(CultureInfo.InvariantCulture));
			}

			pieNode.AppendChild(wedge);
		}

		if (series.PieLabelStyle == PieLabelStyle.Disabled)
		{
			return;
		}

		// Labels after every wedge, so that a label is never covered by the next slice.
		foreach (var slice in slices.Where(s => s.Label.Length > 0))
		{
			if (labelsOutside)
			{
				// No leader line: the reference render draws none, and with the label sitting just clear
				// of the edge there is nothing for one to bridge.
				var to = PointOnCircle(centreX, centreY, radius * 1.05, slice.MidAngleDegrees);

				// Anchored away from the pie, so that the text runs outwards on both sides.
				var onTheRight = Math.Sin(ToRadians(slice.MidAngleDegrees)) >= 0;
				pieNode.AppendChild(
					CreateTextNode(
						FormattableString.Invariant($"pieLabel{slice.StartAngleDegrees:F2}"),
						to.X + (onTheRight ? 3 : -3),
						to.Y,
						slice.Label,
						onTheRight ? HorizontalAlignment.Left : HorizontalAlignment.Right,
						VerticalAlignment.Middle,
						series.FontWeight,
						series.FontFamily,
						series.FontSize,
						Colors.Transparent,
						series.FontColor));
			}
			else
			{
				// Midway through the ring for a doughnut, and two thirds out for a pie, which is
				// where the wedge is widest.
				var labelRadius = innerRadius > 0 ? (radius + innerRadius) / 2 : radius * 0.7;
				var at = PointOnCircle(centreX, centreY, labelRadius, slice.MidAngleDegrees);
				pieNode.AppendChild(
					CreateTextNode(
						FormattableString.Invariant($"pieLabel{slice.StartAngleDegrees:F2}"),
						at.X,
						at.Y,
						slice.Label,
						HorizontalAlignment.Center,
						VerticalAlignment.Middle,
						series.FontWeight,
						series.FontFamily,
						series.FontSize,
						Colors.Transparent,
						series.FontColor));
			}
		}
	}

	/// <summary>
	/// The legend for a pie, which describes slices rather than series.
	/// </summary>
	private void PlotPieLegend(Chart chart, List<PieSlice> slices, XmlElement chartBackgroundAreaNode)
	{
		if (chart.Legends.Count == 0 || slices.Count == 0)
		{
			return;
		}

		var legend = chart.Legends[0];
		var legendXmlElement = GetGroup(legend, "legend", chart.ChartBackgroundArea);
		chartBackgroundAreaNode.AppendChild(legendXmlElement);

		var legendHeight = heightPixels * legend.GetCanvasHeightPercent() / 100;
		var legendWidth = widthPixels * legend.GetCanvasWidthPercent() / 100;
		var fontSize = legend.FontSize;
		var swatchWidth = SwatchWidth(fontSize);
		var swatchSize = SwatchHeight(fontSize);
		var padding = Math.Round(fontSize * 0.5, 2);
		var inset = Math.Round(legendWidth * LegendInsetFraction, 2);

		// A pie legend is a list: one row per slice whatever the legend style, because slices are
		// named and there are usually more of them than a single row would fit. The rows share the
		// legend height the same way a series legend does.

		for (var index = 0; index < slices.Count; index++)
		{
			var slice = slices[index];
			var swatchY = Math.Round(
				RowCentre(legendHeight, index, slices.Count, swatchSize) - (swatchSize / 2),
				2);

			var swatchNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
			swatchNode.SetAttribute("x", inset.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("y", swatchY.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("width", swatchWidth.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("height", swatchSize.ToString(CultureInfo.InvariantCulture));
			swatchNode.SetAttribute("fill", slice.Color.ToHex());
			legendXmlElement.AppendChild(swatchNode);

			legendXmlElement.AppendChild(
				CreateTextNode(
					FormattableString.Invariant($"legendSlice{index}Text"),
					inset + swatchWidth + (padding / 2),
					swatchY + (swatchSize / 2),
					slice.LegendText,
					HorizontalAlignment.Left,
					VerticalAlignment.Middle,
					legend.FontWeight,
					legend.FontFamily,
					fontSize,
					Colors.Transparent,
					legend.FontColor));
		}
	}

	/// <summary>
	/// The path for one wedge: a filled sector for a pie, or a band between two radii for a
	/// doughnut.
	/// </summary>
	private static string WedgePath(double centreX, double centreY, double radius, double innerRadius, PieSlice slice)
	{
		// A single slice covering the whole circle cannot be drawn as one arc, because its start
		// and end points coincide and the arc becomes a no-op. Two half arcs draw it correctly.
		var sweep = Math.Min(slice.SweepAngleDegrees, 360);
		if (sweep >= 359.999)
		{
			return FullRingPath(centreX, centreY, radius, innerRadius);
		}

		var start = slice.StartAngleDegrees;
		var end = start + sweep;
		var largeArc = sweep > 180 ? 1 : 0;

		var outerStart = PointOnCircle(centreX, centreY, radius, start);
		var outerEnd = PointOnCircle(centreX, centreY, radius, end);

		if (innerRadius <= 0)
		{
			return $"M{N(centreX)} {N(centreY)} L{N(outerStart.X)} {N(outerStart.Y)} "
				+ $"A{N(radius)} {N(radius)} 0 {largeArc} 1 {N(outerEnd.X)} {N(outerEnd.Y)} Z";
		}

		var innerEnd = PointOnCircle(centreX, centreY, innerRadius, end);
		var innerStart = PointOnCircle(centreX, centreY, innerRadius, start);

		return $"M{N(outerStart.X)} {N(outerStart.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 {largeArc} 1 {N(outerEnd.X)} {N(outerEnd.Y)} "
			+ $"L{N(innerEnd.X)} {N(innerEnd.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 {largeArc} 0 {N(innerStart.X)} {N(innerStart.Y)} Z";
	}

	private static string FullRingPath(double centreX, double centreY, double radius, double innerRadius)
	{
		var top = PointOnCircle(centreX, centreY, radius, 0);
		var bottom = PointOnCircle(centreX, centreY, radius, 180);
		var outer = $"M{N(top.X)} {N(top.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 1 1 {N(bottom.X)} {N(bottom.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 1 1 {N(top.X)} {N(top.Y)} Z";

		if (innerRadius <= 0)
		{
			return outer;
		}

		// The hole is drawn the other way round, so that the default fill rule leaves it empty.
		var innerTop = PointOnCircle(centreX, centreY, innerRadius, 0);
		var innerBottom = PointOnCircle(centreX, centreY, innerRadius, 180);
		return outer
			+ $" M{N(innerTop.X)} {N(innerTop.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 1 0 {N(innerBottom.X)} {N(innerBottom.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 1 0 {N(innerTop.X)} {N(innerTop.Y)} Z";
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
	/// A point on a circle, at an angle measured clockwise from twelve o'clock.
	/// </summary>
	private static (double X, double Y) PointOnCircle(double centreX, double centreY, double radius, double angleDegrees)
	{
		// A start angle of zero puts the first slice boundary at three o clock, not twelve.
		// Established by rendering four equal quarters through both renderers: the Microsoft
		// chart control put the first quarter between three and six o clock.
		var radians = ToRadians(angleDegrees + QuarterTurnDegrees);
		return (centreX + (radius * Math.Sin(radians)), centreY - (radius * Math.Cos(radians)));
	}

	private static double ToRadians(double degrees) => degrees * Math.PI / 180;

	/// <summary>
	/// Builds the marker shape for a series, centred on the origin so that a single definition can
	/// be placed at each point by translation alone.
	/// </summary>
	/// <remarks>
	/// Issue #30: only Circle was implemented, and every other value of the enum threw
	/// NotSupportedException - so a chart asking for square markers failed outright rather than
	/// rendering. The sizes follow the Microsoft chart control convention that marker size is the
	/// full width of the marker, not its radius.
	/// </remarks>
	private XmlElement? CreateMarkerDefinition(Series series, string id)
	{
		if (series.MarkerStyle == MarkerStyle.None)
		{
			return null;
		}

		// The Microsoft control treats marker size as the overall width; a circle takes half of
		// it as its radius, which is what the original Circle case did with StrokeWidth.
		var size = series.MarkerSize ?? series.StrokeWidth * 2;
		var half = size / 2;

		var fill = (series.MarkerFillColor ?? series.FillColor).ToHex();
		var stroke = (series.MarkerStrokeColor ?? series.StrokeColor).ToHex();
		var strokeWidth = (series.MarkerStrokeWidth ?? series.StrokeWidth).ToString(CultureInfo.InvariantCulture);

		XmlElement node;
		switch (series.MarkerStyle)
		{
			case MarkerStyle.Circle:
				node = _xmlDocument.CreateElement(string.Empty, "circle", string.Empty);
				node.SetAttribute("r", N(half));
				break;

			case MarkerStyle.Square:
				node = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
				node.SetAttribute("x", N(-half));
				node.SetAttribute("y", N(-half));
				node.SetAttribute("width", N(size));
				node.SetAttribute("height", N(size));
				break;

			case MarkerStyle.Diamond:
				node = Polygon([(0, -half), (half, 0), (0, half), (-half, 0)]);
				break;

			case MarkerStyle.Triangle:
				// Centred on its centroid rather than its bounding box, so it sits on the point
				// the way the other shapes do.
				node = Polygon([(0, -half), (half, half * 0.6), (-half, half * 0.6)]);
				break;

			case MarkerStyle.Cross:
				// A plus sign of the same width as the other markers, one third of it thick.
				var arm = half / 3;
				node = Polygon(
				[
					(-arm, -half), (arm, -half), (arm, -arm), (half, -arm),
					(half, arm), (arm, arm), (arm, half), (-arm, half),
					(-arm, arm), (-half, arm), (-half, -arm), (-arm, -arm)
				]);
				break;

			case MarkerStyle.Star4:
				node = Star(4, half);
				break;

			case MarkerStyle.Star5:
				node = Star(5, half);
				break;

			case MarkerStyle.Star6:
				node = Star(6, half);
				break;

			case MarkerStyle.Star10:
				node = Star(10, half);
				break;

			default:
				throw new NotSupportedException($"Marker type {series.MarkerStyle} is not supported.");
		}

		node.SetAttribute("id", id);
		node.SetAttribute("fill", fill);
		node.SetAttribute("stroke", stroke);
		node.SetAttribute("stroke-width", strokeWidth);
		return node;
	}

	/// <summary>
	/// A closed polygon through the given points, which are relative to the marker centre.
	/// </summary>
	private XmlElement Polygon(IReadOnlyList<(double X, double Y)> points)
	{
		var node = _xmlDocument.CreateElement(string.Empty, "polygon", string.Empty);
		node.SetAttribute("points", string.Join(" ", points.Select(p => $"{N(p.X)},{N(p.Y)}")));
		return node;
	}

	/// <summary>
	/// A star with the given number of points, alternating between the outer radius and an inner
	/// one at 40% of it.
	/// </summary>
	private XmlElement Star(int points, double outerRadius)
	{
		var innerRadius = outerRadius * 0.4;
		var vertices = new List<(double X, double Y)>();

		for (var index = 0; index < points * 2; index++)
		{
			// Starting at twelve o'clock so a five-pointed star sits upright.
			var angle = (index * Math.PI / points) - (Math.PI / 2);
			var radius = index % 2 == 0 ? outerRadius : innerRadius;
			vertices.Add((radius * Math.Cos(angle), radius * Math.Sin(angle)));
		}

		return Polygon(vertices);
	}
	/// <summary>
	/// How far in from the left of the legend an entry starts, as a fraction of its width.
	/// </summary>
	/// <remarks>
	/// Measured: 18 pixels into a 144-wide legend and 9 into an 86-wide one, so it scales with the
	/// legend rather than with the font. Scaling matters beyond fidelity - a fixed inset made
	/// LegendWidthPercent change nothing visible, so a chart could ask for a wider legend and get
	/// the same one.
	/// </remarks>
	private const double LegendInsetFraction = 0.12;

	/// <summary>
	/// The height of a legend swatch for a given font size.
	/// </summary>
	/// <remarks>
	/// Measured on two legends against the reference render: a 12-point legend drew swatches 32 by
	/// 14 and a 20-point one 52 by 23. Both are close to 2.6 and 1.15 times the font size, and the
	/// shape matters - a square swatch, which is what this drew, is less than a third of the area
	/// and reads as a different chart.
	/// </remarks>
	private static double SwatchHeight(double fontSize) => Math.Round(fontSize * 1.15, 2);

	/// <summary>
	/// The width of a legend swatch for a given font size.
	/// </summary>
	private static double SwatchWidth(double fontSize) => Math.Round(fontSize * 2.6, 2);

	/// <summary>
	/// A legend entry's text: its legend text where it has one, and its name otherwise.
	/// </summary>
	private static string LegendTextFor(Series series)
		=> series.LegendText is { Length: > 0 } ? series.LegendText : series.Name;

	/// <summary>
	/// How wide a piece of text will be, near enough to lay a row out with.
	/// </summary>
	/// <remarks>
	/// An estimate from the character count, because there is no text measurement here - the SVG
	/// is written out rather than drawn, so nothing in this library knows a font's metrics. It is
	/// good enough to stop entries colliding, which is what it is for; it is not good enough to
	/// match a reference render to the pixel, and legend width fidelity is limited by that.
	/// </remarks>
	private static double EstimateTextWidth(string text, double fontSize)
		=> text.Length * fontSize * 0.55;
	/// <summary>
	/// The centre of one legend row, for entries sharing the legend height equally.
	/// </summary>
	/// <remarks>
	/// The rows are spread rather than packed: the reference render spaced three entries 129 apart
	/// and two 193 apart on a 400-pixel legend, which is the height less one swatch divided by the
	/// count, with the block centred.
	/// </remarks>
	private static double RowCentre(double legendHeight, int index, int count, double swatchHeight)
	{
		var spacing = (legendHeight - swatchHeight) / Math.Max(count, 1);
		return (legendHeight / 2) + ((index - ((count - 1) / 2.0)) * spacing);
	}
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
		var groupNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		groupNode.SetAttribute("id", id);

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
