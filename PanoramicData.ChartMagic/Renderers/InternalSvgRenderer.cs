namespace PanoramicData.ChartMagic.Renderers;

internal class InternalSvgRenderer
{
	private readonly XmlDocument _xmlDocument;
	private readonly int _widthPixels;
	private readonly int _heightPixels;
	private readonly bool _debug;

	public InternalSvgRenderer(int widthPixels, int heightPixels, bool debug)
	{
		_xmlDocument = new XmlDocument();
		_widthPixels = widthPixels;
		_heightPixels = heightPixels;
		_debug = debug;
	}

	internal void SaveImage(Stream stream, Chart chart)
	{
		Initialize(
			chart,
			out var defs,
			out var chartBackgroundAreaNode,
			out var chartAreaNode,
			out var innerPlotNode,
			out var axisHandlerResult);

		PlotSeries(
			chart,
			axisHandlerResult,
			defs,
			innerPlotNode);

		PlotAxes(
			chart,
			chartAreaNode,
			axisHandlerResult);

		PlotLegends(
			chart,
			chartBackgroundAreaNode);

		PlotAnnotations(
			chart,
			chartBackgroundAreaNode);

		var writer = new XmlTextWriter(stream, Encoding.Unicode)
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
		var xmlDeclaration = _xmlDocument.CreateXmlDeclaration("1.0", "UTF-16", "yes");
		var root = _xmlDocument.DocumentElement;
		_xmlDocument.InsertBefore(xmlDeclaration, root);

		var svg = _xmlDocument.CreateElement(string.Empty, "svg", string.Empty);
		svg.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
		svg.SetAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
		_xmlDocument.AppendChild(svg);
		svg.SetAttribute("width", _widthPixels.ToString(CultureInfo.InvariantCulture));
		svg.SetAttribute("height", _heightPixels.ToString(CultureInfo.InvariantCulture));

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
				chart.ChartBackgroundArea,
				$"annotation{annotationIndex++}",
				annotation.GetCanvasXLocationPercent(),
				annotation.GetCanvasYLocationPercent(),
				annotation.Text,
				annotation.HorizontalAlignment,
				annotation.VerticalAlignment,
				annotation.FontWeight,
				annotation.FontFamily,
				annotation.StrokeColor,
				annotation.FillColor);
			chartBackgroundAreaNode.AppendChild(textNode);
		}
	}

	private XmlElement CreateTextNode(
		ChartNamedElement chartNamedElement,
		string id,
		double xPositionPercent,
		double yPositionPercent,
		string text,
		HorizontalAlignment horizontalAlignment,
		VerticalAlignment verticalAlignment,
		FontWeight fontWeight,
		string? fontFamily,
		Color strokeColor,
		Color fillColor
		)
	{
		var textNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
		textNode.SetAttribute("x", GetRelativePositionX(chartNamedElement, xPositionPercent).ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("y", GetRelativePositionY(chartNamedElement, yPositionPercent).ToString(CultureInfo.InvariantCulture));
		textNode.SetAttribute("id", id);
		textNode.InnerText = text;
		textNode.SetAttribute("text-anchor", horizontalAlignment switch
		{
			HorizontalAlignment.Left => "start",
			HorizontalAlignment.Center => "middle",
			HorizontalAlignment.Right => "end",
			_ => throw new NotSupportedException($"Unsupported HorizontalAlignment '{horizontalAlignment}'")
		});
		textNode.SetAttribute("alignment-baseline", verticalAlignment switch
		{
			VerticalAlignment.Top => "hanging",
			VerticalAlignment.Middle => "middle",
			VerticalAlignment.Bottom => "baseline",
			_ => throw new NotSupportedException($"Unsupported VerticalAlignment '{verticalAlignment}'")
		});
		textNode.SetAttribute("font-weight", fontWeight.ToString().ToLowerInvariant());
		textNode.SetAttribute("font-family", fontFamily);
		textNode.SetAttribute("stroke", strokeColor.ToHex());
		textNode.SetAttribute("fill", fillColor.ToHex());

		return textNode;
	}

	private double GetRelativePositionY(ChartNamedElement chartNamedElement, double yPositionPercent)
		=> (_heightPixels * ((100 - yPositionPercent * chartNamedElement.GetCanvasHeightPercent() / 100)) / 100);
	private double GetRelativePositionX(ChartNamedElement chartNamedElement, double xPositionPercent)
		=> (_widthPixels * xPositionPercent * chartNamedElement.GetCanvasWidthPercent() / 100 / 100);

	private void PlotLegends(Chart chart, XmlElement chartBackgroundAreaNode)
	{
		// Legends
		if (chart.Legends.Count > 0)
		{
			var legend = chart.Legends[0];
			var legendXmlElement = GetGroup(legend, "legend");
			chartBackgroundAreaNode.AppendChild(legendXmlElement);

			var seriesIndex = 0;
			double
				textXPositionPercent,
				textYPositionPercent,
				rectXPositionPercent,
				rectYPositionPercent;
			foreach (var series in chart.Series)
			{
				switch (legend.Style)
				{
					case LegendStyle.Row:
						textXPositionPercent = 15 + 75 * (seriesIndex + 1) / (chart.Series.Count + 1);
						textYPositionPercent = 50;
						rectXPositionPercent = 10 + 75 * (seriesIndex + 1) / (chart.Series.Count + 1);
						rectYPositionPercent = 48;
						break;
					case LegendStyle.Column:
						textXPositionPercent = 30;
						textYPositionPercent = 8 + 80 * (seriesIndex + 1) / (chart.Series.Count + 1);
						rectXPositionPercent = 10;
						rectYPositionPercent = 10 + 80 * (seriesIndex + 1) / (chart.Series.Count + 1);
						break;
					default:
						throw new NotSupportedException($"Legend style {legend.Style} not supported.");
				};
				// Add legend series text
				var seriesSymbolXmlNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
				seriesSymbolXmlNode.SetAttribute("x", GetRelativePositionX(legend, rectXPositionPercent).ToString(CultureInfo.InvariantCulture));
				seriesSymbolXmlNode.SetAttribute("y", GetRelativePositionY(legend, rectYPositionPercent).ToString(CultureInfo.InvariantCulture));
				seriesSymbolXmlNode.SetAttribute("fill", series.FillColor.ToHex());
				seriesSymbolXmlNode.SetAttribute("stroke", series.StrokeColor.ToHex());
				seriesSymbolXmlNode.SetAttribute("width", "4%");
				seriesSymbolXmlNode.SetAttribute("height", series.ChartType switch { SeriesChartType.Line => "2%", _ => "4%" });
				legendXmlElement.AppendChild(seriesSymbolXmlNode);
				legendXmlElement.AppendChild(
					CreateTextNode(
						legend,
						$"legendSeries{seriesIndex}Text",
						textXPositionPercent,
						textYPositionPercent,
						series.Name,
						HorizontalAlignment.Left,
						VerticalAlignment.Middle,
						legend.FontWeight,
						legend.FontFamily,
						legend.FontColor,
						legend.FillColor)
					);
				seriesIndex++;
			}
		}
	}

	private void PlotAxes(Chart chart, XmlElement chartAreaNode, AxisHandlerResult axisHandlerResult)
	{
		// X Axis
		var xAxisNode = GetGroup(chart.ChartArea.XAxis, "xAxis");
		chartAreaNode.AppendChild(xAxisNode);

		// Y Axis
		var yAxisNode = GetGroup(chart.ChartArea.YAxis, "yAxis");
		chartAreaNode.AppendChild(yAxisNode);
	}

	private void PlotSeries(Chart chart, AxisHandlerResult axisHandlerResult, XmlElement defs, XmlElement innerPlotNode)
	{
		// Determine axis locations
		var xAxisDisplayStart = chart.ChartArea.XAxis.Min ?? axisHandlerResult.MinX ?? 0;
		var xAxisDisplayEnd = chart.ChartArea.XAxis.Max ?? axisHandlerResult.MaxX ?? 0;
		var xAxisDisplayRange = xAxisDisplayEnd - xAxisDisplayStart;
		var yAxisDisplayStart = chart.ChartArea.YAxis.Min ?? axisHandlerResult.MinY ?? 0;
		var yAxisDisplayEnd = chart.ChartArea.YAxis.Max ?? axisHandlerResult.MaxY ?? 0;
		var yAxisDisplayRange = yAxisDisplayEnd - yAxisDisplayStart;

		var innerPlotHeight = _heightPixels * chart.ChartArea.InnerPlot.GetCanvasHeightPercent() / 100;
		var innerPlotWidth = _widthPixels * chart.ChartArea.InnerPlot.GetCanvasWidthPercent() / 100;
		var stackedColumnDictionary = new Dictionary<string, double>();
		var stackedAreaDictionary = new Dictionary<string, double>();
		var lastStackedColumnDictionary = new Dictionary<string, double>();
		var lastStackedAreaDictionary = new Dictionary<string, double>();
		var stackLines = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		stackLines.SetAttribute("id", "stackLines");
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
					throw new NotSupportedException($"Marker type {series.MarkerStyle} not supported.");
			}

			var stackDictionary = series.ChartType switch
			{
				SeriesChartType.StackedColumn => stackedColumnDictionary,
				SeriesChartType.StackedArea => stackedAreaDictionary,
				_ => null
			};

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
				var xValueString = xValue.ToString() ?? string.Empty;
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

				// simple left to right, equidistanced for now
				var xPosition = Math.Round(innerPlotWidth * (xValue - xAxisDisplayStart) / xAxisDisplayRange, 2);
				var yPosition = Math.Round(innerPlotHeight * (1 - ((yValue - yAxisDisplayStart) / yAxisDisplayRange)), 2);
				if (previousYValue is not null)
				{
					var previousYPosition = Math.Round(innerPlotHeight * (1 - (((double)previousYValue - yAxisDisplayStart) / yAxisDisplayRange)), 2);
					returnPathPoints.Add(new Tuple<double, double>(xPosition, previousYPosition));
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
					// Store lastStackedAreaDictionary
					lastStackedAreaDictionary = new();
					foreach (var key in stackedAreaDictionary.Keys)
					{
						lastStackedAreaDictionary[key] = stackedAreaDictionary[key];
					}
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

	private string? GetXPosition(double xPositionPercent)
		=> Math.Round(_widthPixels * xPositionPercent / 100, 2).ToString(CultureInfo.InvariantCulture);

	private string? GetYPosition(double yPositionPercent)
		=> Math.Round(_heightPixels * (100 - yPositionPercent) / 100, 2).ToString(CultureInfo.InvariantCulture);

	private XmlElement GetGroup(ChartNamedElement element, string id)
	{
		var groupNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		groupNode.SetAttribute("id", id);
		var inverseYPositionPercent = element.GetCanvasYLocationPercent() + element.GetCanvasHeightPercent();
		var translation = $"{_widthPixels * element.GetCanvasXLocationPercent() / 100},{_heightPixels * (100 - (inverseYPositionPercent)) / 100}";
		if (translation != "0,0")
		{
			groupNode.SetAttribute("transform", $"translate({translation})");
		}
		var rectNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
		rectNode.SetAttribute("width", (_widthPixels * element.GetCanvasWidthPercent() / 100).ToString(CultureInfo.InvariantCulture));
		rectNode.SetAttribute("height", (_heightPixels * element.GetCanvasHeightPercent() / 100).ToString(CultureInfo.InvariantCulture));
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

		if (_debug)
		{
			var debugTextNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
			debugTextNode.SetAttribute("alignment-baseline", "hanging");
			debugTextNode.InnerText = element.Name;
			groupNode.AppendChild(debugTextNode);
		}

		return groupNode;
	}
}
