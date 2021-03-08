using PanoramicData.ChartMagic.Extensions;
using PanoramicData.ChartMagic.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace PanoramicData.ChartMagic.Renderers
{
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
			XmlElement root = _xmlDocument.DocumentElement;
			_xmlDocument.InsertBefore(xmlDeclaration, root);

			XmlElement svg = _xmlDocument.CreateElement(string.Empty, "svg", string.Empty);
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

			// Inner Plot background
			var innerPlotNode = GetGroup(chart.ChartArea.InnerPlot, totalHeight);
			chartAreaNode.AppendChild(innerPlotNode);

			// X Axis
			var xAxisNode = GetGroup(chart.ChartArea.XAxis, totalHeight);
			chartAreaNode.AppendChild(xAxisNode);

			// Y Axis
			var yAxisNode = GetGroup(chart.ChartArea.YAxis, totalHeight);
			chartAreaNode.AppendChild(yAxisNode);

			var maxPointIndex = axisHandlerResult.MaxXCount;
			foreach (var series in chart.Series)
			{
				var pointIndex = 0;
				foreach (var chartPoint in series.Points)
				{
					// simple left to right, equidistanced for now
					var xPositionPercent = 100 * pointIndex++ / maxPointIndex;
				}
			}

			// Legends
			if (chart.Legends.Count > 0)
			{
				var legend = chart.Legends[0];
				var legendNode = GetGroup(legend, totalHeight);
				chartBackgroundAreaNode.AppendChild(legendNode);
			}

			// Labels

			// Annotations

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
}
