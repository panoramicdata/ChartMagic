using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers;

internal class InternalSvgRenderer(int widthPixels, int heightPixels, bool debug)
{
	private readonly XmlDocument _xmlDocument = new();

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

		// Gridlines first, so that the series are drawn over them rather than under.
		PlotGridlines(chart, geometry, innerPlotNode);

		PlotSeries(chart, geometry, defs, innerPlotNode);

		PlotAxes(chart, geometry, chartAreaNode);

		PlotLegends(chart, chartBackgroundAreaNode);

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
		chartAreaNode = GetGroup(chart.ChartArea, "chartArea");
		chartBackgroundAreaNode.AppendChild(chartAreaNode);

		// Inner Plot background
		innerPlotNode = GetGroup(chart.ChartArea.InnerPlot, "innerPlot");
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
		var roundedX = Math.Round(x, 2);
		var roundedY = Math.Round(y, 2);

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
		textNode.SetAttribute("alignment-baseline", verticalAlignment switch
		{
			VerticalAlignment.Top => "hanging",
			VerticalAlignment.Middle => "middle",
			VerticalAlignment.Bottom => "baseline",
			_ => throw new NotSupportedException($"Unsupported VerticalAlignment {verticalAlignment}.")
		});
		textNode.SetAttribute("font-weight", fontWeight.ToString().ToLowerInvariant());
		textNode.SetAttribute("font-size", fontSize.ToString(CultureInfo.InvariantCulture));
		if (fontFamily is { Length: > 0 })
		{
			textNode.SetAttribute("font-family", fontFamily);
		}

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
		var legendXmlElement = GetGroup(legend, "legend");
		chartBackgroundAreaNode.AppendChild(legendXmlElement);

		var legendWidth = widthPixels * legend.GetCanvasWidthPercent() / 100;
		var legendHeight = heightPixels * legend.GetCanvasHeightPercent() / 100;
		var fontSize = legend.FontSize;
		var swatchSize = Math.Round(fontSize * 0.8, 2);
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
						// One slot per series across the legend width.
						var slotWidth = (legendWidth - (2 * padding)) / chart.Series.Count;
						swatchX = padding + (seriesIndex * slotWidth);
						swatchY = Math.Round((legendHeight - swatchSize) / 2, 2);
						break;
					}

				case LegendStyle.Column:
					{
						// One row per series, stacked from the top and centred as a block.
						var lineHeight = fontSize * 1.6;
						var blockHeight = lineHeight * chart.Series.Count;
						var top = Math.Max(padding, (legendHeight - blockHeight) / 2);
						swatchX = padding;
						swatchY = Math.Round(top + (seriesIndex * lineHeight) + ((lineHeight - swatchSize) / 2), 2);
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
			swatchNode.SetAttribute("width", swatchSize.ToString(CultureInfo.InvariantCulture));
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
					swatchX + swatchSize + (padding / 2),
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
			foreach (var value in MinorTicks(xAxis, geometry, isValueAxis: geometry.IsHorizontalPlot))
			{
				var x = XAxisPixels(geometry, value);
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
		var xAxisNode = GetGroup(xAxis, "xAxis");
		chartAreaNode.AppendChild(xAxisNode);
		if (xAxis.IsEnabled && xAxis.LabelsEnabled)
		{
			DrawXAxis(chart, geometry, xAxis, xAxisNode);
		}

		// Y Axis
		var yAxis = chart.ChartArea.YAxis;
		var yAxisNode = GetGroup(yAxis, "yAxis");
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

		xAxisNode.AppendChild(CreateLine(0, 0, geometry.Width, 0, xAxis.LineColor, xAxis.LineWidth));

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

		yAxisNode.AppendChild(CreateLine(axisWidth, 0, axisWidth, geometry.Height, yAxis.LineColor, yAxis.LineWidth));

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
				chart.ChartArea.YAxis.Interval,
				chart.ChartArea.YAxis.TargetTickCount);
	}

	/// <summary>
	/// Minor gridline positions: the interval the caller gave, otherwise a subdivision of the
	/// major interval, or the intermediate steps within each decade on a logarithmic axis.
	/// </summary>
	private static IReadOnlyList<double> MinorTicks(AxisArea axis, PlotGeometry geometry, bool isValueAxis)
	{
		if (!isValueAxis)
		{
			// Minor gridlines have no meaning on a category axis: there is nothing between one
			// category and the next.
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
			var absolute = Math.Abs(value);
			if (absolute >= 1_000_000_000)
			{
				return FormattableString.Invariant($"{value / 1_000_000_000:0.##}bn");
			}

			if (absolute >= 1_000_000)
			{
				return FormattableString.Invariant($"{value / 1_000_000:0.##}M");
			}

			if (absolute >= 1_000)
			{
				return FormattableString.Invariant($"{value / 1_000:0.##}k");
			}
		}

		// Two decimal places at most, and none where the value does not need them.
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private XmlElement CreateLine(double x1, double y1, double x2, double y2, Color color, double width)
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
		return lineNode;
	}

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
			switch (series.MarkerStyle)
			{
				case MarkerStyle.Circle:
					var circleDef = _xmlDocument.CreateElement(string.Empty, "circle", string.Empty);
					circleDef.SetAttribute("id", seriesMarkerId);
					circleDef.SetAttribute("r", (series.MarkerSize is not null ? series.MarkerSize : series.StrokeWidth).ToString());
					circleDef.SetAttribute("fill", $"{(series.MarkerFillColor ?? series.FillColor).ToHex()}");
					circleDef.SetAttribute("stroke", $"{(series.MarkerStrokeColor ?? series.StrokeColor).ToHex()}");
					circleDef.SetAttribute("stroke-width", $"{series.MarkerStrokeWidth ?? series.StrokeWidth}");
					defs.AppendChild(circleDef);
					break;
				case MarkerStyle.None:
					break;
				default:
					throw new NotSupportedException($"Marker type {series.MarkerStyle} is not supported.");
			}

			var stackDictionary = series.ChartType switch
			{
				SeriesChartType.StackedColumn => stackedColumnDictionary,
				SeriesChartType.StackedBar => stackedColumnDictionary,
				SeriesChartType.StackedArea => stackedAreaDictionary,
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
			var areaStringBuilder = new StringBuilder($"M0 {innerPlotHeight}");
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
					yValue = (double)(yPointValue! + (previousYValue ?? 0));
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
					if (returnPathPoints.Count == 0)
					{
						returnPathPoints.Add(
							new(
								innerPlotWidth,
								innerPlotHeight
							)
						);
					}

					areaStringBuilder.Append(string.Join("", returnPathPoints.AsEnumerable().Reverse().Select(p => $"L{p.Item1} {p.Item2}")));
					areaStringBuilder.Append('Z');
					areaNode.SetAttribute("d", areaStringBuilder.ToString());
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
				var newTotal = previousTotal + chartPoint.YValue.Value;
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

	private XmlElement GetGroup(ChartNamedElement element, string id)
	{
		var groupNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		groupNode.SetAttribute("id", id);
		var inverseYPositionPercent = element.GetCanvasYLocationPercent() + element.GetCanvasHeightPercent();
		var translation = $"{widthPixels * element.GetCanvasXLocationPercent() / 100},{heightPixels * (100 - (inverseYPositionPercent)) / 100}";
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
