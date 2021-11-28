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
		_xmlDocument.AppendChild(svg);
		svg.SetAttribute("width", chart.ChartBackgroundArea.Width.ToString(CultureInfo.InvariantCulture));
		var totalHeight = chart.ChartBackgroundArea.Height;
		svg.SetAttribute("height", totalHeight.ToString(CultureInfo.InvariantCulture));

		// Chart background area
		var chartBackgroundAreaNode = GetGroup(chart.ChartBackgroundArea, totalHeight);
		svg.AppendChild(chartBackgroundAreaNode);

		// ChartArea background
		var chartAreaNode = GetGroup(chart.ChartArea, totalHeight);
		chartBackgroundAreaNode.AppendChild(chartAreaNode);

		// X Axis
		var xAxisNode = GetGroup(chart.ChartArea.XAxis, totalHeight);
		chartAreaNode.AppendChild(xAxisNode);

		// Y Axis
		var yAxisNode = GetGroup(chart.ChartArea.YAxis, totalHeight);
		chartAreaNode.AppendChild(yAxisNode);

		// Inner Plot background
		var innerPlotNode = GetGroup(chart.ChartArea.InnerPlot, totalHeight);
		chartAreaNode.AppendChild(innerPlotNode);

		var maxPointIndex = axisHandlerResult.MaxXCount;

		// TODO - snap to axis grid lines
		var yAxisDisplayStart = chart.ChartArea.YAxis.Min ?? axisHandlerResult.MinYDouble;
		var yAxisDisplayEnd = chart.ChartArea.YAxis.Max ?? axisHandlerResult.MaxYDouble;
		var yAxisDisplayRange = yAxisDisplayEnd - yAxisDisplayStart;

		// Series
		var innerPlotHeight = chart.ChartArea.InnerPlot.Height;
		foreach (var series in chart.Series)
		{
			var pointIndex = 0;
			var pathNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
			var areaNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
			var pathStringBuilder = new StringBuilder();
			var areaStringBuilder = new StringBuilder($"M0 {innerPlotHeight}");
			var lastXPosition = (float?)null;
			var isFirstPoint = true;
			foreach (var chartPoint in series.Points)
			{
				// simple left to right, equidistanced for now
				var xPosition = lastXPosition = chart.ChartArea.InnerPlot.Width * pointIndex++ / maxPointIndex;
				var yPosition = innerPlotHeight * (1 - ((chartPoint.YValue - yAxisDisplayStart) / yAxisDisplayRange));
				// Letter - always M to start, afterwards L unless the previous value is null
				pathStringBuilder.Append($"{(isFirstPoint ? "M" : " L")}{xPosition} {yPosition}");
				areaStringBuilder.Append($" L{xPosition} {yPosition}");
				isFirstPoint = false;
			}

			// Fill Area
			switch (series.ChartType)
			{
				case SeriesChartType.Area:
					areaStringBuilder.Append($"L {lastXPosition} {innerPlotHeight} Z");
					areaNode.SetAttribute("d", areaStringBuilder.ToString());
					areaNode.SetAttribute("style", $"stroke:none; fill:{series.FillColor.ToHex()};");
					innerPlotNode.AppendChild(areaNode);
					break;
			}

			// Line
			switch (series.ChartType)
			{
				case SeriesChartType.Area:
				case SeriesChartType.Line:
					pathNode.SetAttribute("d", pathStringBuilder.ToString());
					pathNode.SetAttribute("style", $"stroke:{series.StrokeColor.ToHex()}; fill:none; stroke-width:{series.StrokeWidth};");
					innerPlotNode.AppendChild(pathNode);
					break;
			}
		}

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

	private XmlElement GetGroup(ChartNamedElement element, float totalHeight)
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
		var style = new List<string>();
		if (element.FillColor != Colors.Transparent)
		{
			style.Add($"fill:{element.FillColor.ToHex()}");
			if (element.FillColor.A != 255)
			{
				style.Add($"opacity:{(element.FillColor.A / 255f).ToString("F2", CultureInfo.InvariantCulture)}");
			}
		}
		if (element.StrokeColor != Colors.Transparent && element.StrokeWidth != 0)
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
			style.Add($"stroke-width:{element.StrokeWidth.ToString(CultureInfo.InvariantCulture)}");
		}
		if (style.Count != 0)
		{
			rectNode.SetAttribute("style", string.Join(";", style));
		}
		groupNode.AppendChild(rectNode);

		if (_debug)
		{
			var debugTextNode = _xmlDocument.CreateElement(string.Empty, "text", string.Empty);
			debugTextNode.SetAttribute("y", "20");
			debugTextNode.InnerText = element.Name;
			groupNode.AppendChild(debugTextNode);
		}

		return groupNode;
	}
}
