namespace PanoramicData.ChartMagic.Renderers;

internal class InternalSvgRenderer
{
	private readonly XmlDocument _xmlDocument;
	private readonly bool _debug;

	public InternalSvgRenderer(bool debug)
	{
		_xmlDocument = new XmlDocument();
		_debug = debug;
	}

	internal void SaveImage(Stream stream, Chart chart)
	{
		// Process Series
		var axisHandler = new AxisHandler(chart);
		var axisHandlerResult = axisHandler.Process();

		var xmlDeclaration = _xmlDocument.CreateXmlDeclaration("1.0", "UTF-16", "yes");
		var root = _xmlDocument.DocumentElement;
		_xmlDocument.InsertBefore(xmlDeclaration, root);

		var svg = _xmlDocument.CreateElement(string.Empty, "svg", string.Empty);
		svg.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
		svg.SetAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
		_xmlDocument.AppendChild(svg);
		svg.SetAttribute("width", chart.ChartBackgroundArea.Width.ToString(CultureInfo.InvariantCulture));
		var totalHeight = chart.ChartBackgroundArea.Height;
		svg.SetAttribute("height", totalHeight.ToString(CultureInfo.InvariantCulture));

		// Always define a defs node
		var defs = _xmlDocument.CreateElement(string.Empty, "defs", string.Empty);
		svg.AppendChild(defs);

		// Chart background area
		var chartBackgroundAreaNode = GetGroup(chart.ChartBackgroundArea, totalHeight);
		svg.AppendChild(chartBackgroundAreaNode);

		// ChartArea background
		var chartAreaNode = GetGroup(chart.ChartArea, totalHeight);
		chartBackgroundAreaNode.AppendChild(chartAreaNode);

		// Inner Plot background
		var innerPlotNode = GetGroup(chart.ChartArea.InnerPlot, totalHeight);
		chartAreaNode.AppendChild(innerPlotNode);

		var maxPointIndex = axisHandlerResult.MaxXCount ?? throw new InvalidOperationException("Cannot graph with a null Max X");

		// Determine axis locations
		var xAxisDisplayStart = chart.ChartArea.XAxis.Min ?? axisHandlerResult.MinX ?? 0;
		var xAxisDisplayEnd = chart.ChartArea.XAxis.Max ?? axisHandlerResult.MaxX ?? 0;
		var xAxisDisplaxRange = xAxisDisplayEnd - xAxisDisplayStart;
		var yAxisDisplayStart = chart.ChartArea.YAxis.Min ?? axisHandlerResult.MinY ?? 0;
		var yAxisDisplayEnd = chart.ChartArea.YAxis.Max ?? axisHandlerResult.MaxY ?? 0;
		var yAxisDisplayRange = yAxisDisplayEnd - yAxisDisplayStart;

		// Series
		var innerPlotHeight = chart.ChartArea.InnerPlot.Height;
		var innerPlotWidth = chart.ChartArea.InnerPlot.Width;
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
				var xPosition = Math.Round(innerPlotWidth * (xValue - xAxisDisplayStart) / xAxisDisplaxRange, 2);
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
		// X Axis
		var xAxisNode = GetGroup(chart.ChartArea.XAxis, totalHeight);
		chartAreaNode.AppendChild(xAxisNode);

		// Y Axis
		var yAxisNode = GetGroup(chart.ChartArea.YAxis, totalHeight);
		chartAreaNode.AppendChild(yAxisNode);

		// Legends
		if (chart.Legends.Count > 0)
		{
			var legend = chart.Legends[0];
			var legendNode = GetGroup(legend, totalHeight);
			chartBackgroundAreaNode.AppendChild(legendNode);
		}

		// Annotations
		foreach (var annotation in chart.Annotations)
		{
			var annotationNode = GetGroup(annotation, totalHeight);
			var textNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
			textNode.InnerText = annotation.Text;
			textNode.SetAttribute("text-anchor", annotation.HorizontalAlignment switch
			{
				HorizontalAlignment.Left => "start",
				HorizontalAlignment.Center => "middle",
				HorizontalAlignment.Right => "end",
				_ => throw new NotSupportedException($"Unsupported HorizontalAlignment '{annotation.HorizontalAlignment}'")
			});
			textNode.SetAttribute("alignment-baseline", annotation.VerticalAlignment switch
			{
				VerticalAlignment.Top => "hanging",
				VerticalAlignment.Middle => "middle",
				VerticalAlignment.Bottom => "baseline",
				_ => throw new NotSupportedException($"Unsupported VerticalAlignment '{annotation.VerticalAlignment}'")
			});
			annotationNode.AppendChild(textNode);
			chartBackgroundAreaNode.AppendChild(annotationNode);
		}

		var writer = new XmlTextWriter(stream, Encoding.Unicode)
		{
			Formatting = Formatting.Indented
		};
		_xmlDocument.WriteContentTo(writer);
		writer.Flush();
	}

	private XmlElement GetGroup(ChartNamedElement element, double totalHeight)
	{
		var groupNode = _xmlDocument.CreateElement(string.Empty, "g", string.Empty);
		var translation = $"{element.XPosition.ToString(CultureInfo.InvariantCulture)},{(totalHeight - element.Height - element.YPosition).ToString(CultureInfo.InvariantCulture)}";
		if (translation != "0,0")
		{
			groupNode.SetAttribute("transform", $"translate({translation})");
		}
		var rectNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
		rectNode.SetAttribute("width", element.Width.ToString(CultureInfo.InvariantCulture));
		rectNode.SetAttribute("height", element.Height.ToString(CultureInfo.InvariantCulture));
		if (element.XRadius != 0)
		{
			rectNode.SetAttribute("rx", element.XRadius.ToString(CultureInfo.InvariantCulture));
		}
		if (element.YRadius != 0)
		{
			rectNode.SetAttribute("ry", element.YRadius.ToString(CultureInfo.InvariantCulture));
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
