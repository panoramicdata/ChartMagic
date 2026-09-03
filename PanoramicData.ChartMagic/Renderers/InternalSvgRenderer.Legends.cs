using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// The legend, whether it describes series or pie slices.
/// </summary>
internal partial class InternalSvgRenderer
{
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

		var metrics = MetricsFor(legend);

		var seriesIndex = 0;
		foreach (var series in chart.Series)
		{
			PlotSeriesLegendEntry(chart, legend, metrics, series, seriesIndex, legendXmlElement);
			seriesIndex++;
		}
	}

	/// <summary>
	/// One legend entry: its swatch and its label.
	/// </summary>
	private void PlotSeriesLegendEntry(
		Chart chart,
		Legend legend,
		LegendMetrics metrics,
		Series series,
		int seriesIndex,
		XmlElement legendXmlElement)
	{
		var (swatchX, swatchY) = SwatchOrigin(chart, legend, metrics, seriesIndex);

		// A line series is represented by a bar rather than a block, so that the legend
		// distinguishes a line from a filled area at a glance.
		var isLine = series.ChartType
			is SeriesChartType.Line
			or SeriesChartType.FastLine
			or SeriesChartType.Spline
			or SeriesChartType.StepLine;
		var swatchHeight = isLine ? Math.Max(2, Math.Round(metrics.SwatchSize / 4, 2)) : metrics.SwatchSize;
		var swatchTop = isLine ? swatchY + ((metrics.SwatchSize - swatchHeight) / 2) : swatchY;

		// A line series carries its identity in its stroke, a filled series in its fill.
		var swatchColor = isLine
			? series.StrokeColor
			: series.FillColor != Colors.Transparent ? series.FillColor : series.StrokeColor;

		var swatchNode = CreateSwatch(swatchX, swatchTop, metrics.SwatchWidth, swatchHeight, swatchColor);
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
				swatchX + metrics.SwatchWidth + (metrics.Padding / 2),
				swatchY + (metrics.SwatchSize / 2),
				LegendTextFor(series),
				HorizontalAlignment.Left,
				VerticalAlignment.Middle,
				LabelStyleFor(legend)));
	}

	/// <summary>
	/// Where a series swatch sits within the legend, which is what the legend style decides.
	/// </summary>
	private static (double X, double Y) SwatchOrigin(Chart chart, Legend legend, LegendMetrics metrics, int seriesIndex)
		=> legend.Style switch
		{
			LegendStyle.Row => RowSwatchOrigin(chart, metrics, seriesIndex),
			LegendStyle.Column => ColumnSwatchOrigin(metrics, seriesIndex, chart.Series.Count),
			_ => throw new NotSupportedException($"Legend style {legend.Style} is not supported.")
		};

	/// <summary>
	/// Where a swatch sits in a single-row legend.
	/// </summary>
	/// <remarks>
	/// Entries packed one after another and the row centred, rather than each given an equal share
	/// of the width.
	///
	/// Spreading them looks tidy on paper and is wrong twice over. It does not match the reference
	/// render, which packs them and centres the result; and once the swatch became a rectangle
	/// rather than a small square, a slot sized without reference to its contents put the next
	/// swatch on top of the previous label.
	/// </remarks>
	private static (double X, double Y) RowSwatchOrigin(Chart chart, LegendMetrics metrics, int seriesIndex)
	{
		var entryWidths = chart.Series
			.Select(series => metrics.SwatchWidth
				+ (metrics.Padding / 2)
				+ EstimateTextWidth(LegendTextFor(series), metrics.FontSize))
			.ToList();

		var gap = metrics.Padding * 2;
		var rowWidth = entryWidths.Sum() + (gap * (entryWidths.Count - 1));

		var x = Math.Round(
			Math.Max(metrics.Padding, (metrics.Width - rowWidth) / 2)
				+ entryWidths.Take(seriesIndex).Sum()
				+ (gap * seriesIndex),
			2);
		var y = Math.Round((metrics.Height - metrics.SwatchSize) / 2, 2);
		return (x, y);
	}

	/// <summary>
	/// Where a swatch sits in a single-column legend.
	/// </summary>
	/// <remarks>
	/// One row per series, spread down the legend rather than packed together, and left-aligned at
	/// an inset proportional to the legend width.
	///
	/// Both measured against the renderer this matches, which gives each entry an equal share of
	/// the legend height: on a 400-pixel legend it spaced three entries 129 apart and two 193
	/// apart, which is the height less one swatch, divided by the count. Packing them at 1.6 line
	/// heights put all three within 50 pixels of the middle and left most of the legend empty.
	/// </remarks>
	private static (double X, double Y) ColumnSwatchOrigin(LegendMetrics metrics, int index, int count)
		=> (
			Math.Round(metrics.Width * LegendInsetFraction, 2),
			Math.Round(
				RowCentre(metrics.Height, index, count, metrics.SwatchSize) - (metrics.SwatchSize / 2),
				2));

	/// <summary>
	/// The legend for a pie, which describes slices rather than series.
	/// </summary>
	/// <remarks>
	/// A pie legend is a list: one row per slice whatever the legend style, because slices are
	/// named and there are usually more of them than a single row would fit. The rows share the
	/// legend height the same way a series legend does.
	/// </remarks>
	private void PlotPieLegend(Chart chart, List<PieSlice> slices, XmlElement chartBackgroundAreaNode)
	{
		if (chart.Legends.Count == 0 || slices.Count == 0)
		{
			return;
		}

		var legend = chart.Legends[0];
		var legendXmlElement = GetGroup(legend, "legend", chart.ChartBackgroundArea);
		chartBackgroundAreaNode.AppendChild(legendXmlElement);

		var metrics = MetricsFor(legend);
		var inset = Math.Round(metrics.Width * LegendInsetFraction, 2);
		var labelStyle = LabelStyleFor(legend);

		for (var index = 0; index < slices.Count; index++)
		{
			var slice = slices[index];
			var swatchY = Math.Round(
				RowCentre(metrics.Height, index, slices.Count, metrics.SwatchSize) - (metrics.SwatchSize / 2),
				2);

			legendXmlElement.AppendChild(
				CreateSwatch(inset, swatchY, metrics.SwatchWidth, metrics.SwatchSize, slice.Color));

			legendXmlElement.AppendChild(
				CreateTextNode(
					FormattableString.Invariant($"legendSlice{index}Text"),
					inset + metrics.SwatchWidth + (metrics.Padding / 2),
					swatchY + (metrics.SwatchSize / 2),
					slice.LegendText,
					HorizontalAlignment.Left,
					VerticalAlignment.Middle,
					labelStyle));
		}
	}

	/// <summary>
	/// A legend swatch rectangle, filled but not outlined.
	/// </summary>
	private XmlElement CreateSwatch(double x, double y, double width, double height, Color color)
	{
		var swatchNode = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
		swatchNode.SetAttribute("x", x.ToString(CultureInfo.InvariantCulture));
		swatchNode.SetAttribute("y", y.ToString(CultureInfo.InvariantCulture));
		swatchNode.SetAttribute("width", width.ToString(CultureInfo.InvariantCulture));
		swatchNode.SetAttribute("height", height.ToString(CultureInfo.InvariantCulture));
		swatchNode.SetAttribute("fill", color.ToHex());
		return swatchNode;
	}

	/// <summary>
	/// The pixel measurements a legend is laid out in.
	/// </summary>
	private LegendMetrics MetricsFor(Legend legend)
	{
		var fontSize = legend.FontSize;
		return new LegendMetrics(
			Width: widthPixels * legend.GetCanvasWidthPercent() / 100,
			Height: heightPixels * legend.GetCanvasHeightPercent() / 100,
			FontSize: fontSize,
			SwatchWidth: SwatchWidth(fontSize),
			SwatchSize: SwatchHeight(fontSize),
			Padding: Math.Round(fontSize * 0.5, 2));
	}

	/// <summary>
	/// The style a legend label is drawn in.
	/// </summary>
	private static TextStyle LabelStyleFor(Legend legend)
		=> TextStyle.Unstroked(legend.FontWeight, legend.FontFamily, legend.FontSize, legend.FontColor);

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
	/// The pixel measurements a legend is laid out in.
	/// </summary>
	/// <param name="Width">The legend width, in pixels.</param>
	/// <param name="Height">The legend height, in pixels.</param>
	/// <param name="FontSize">The legend font size.</param>
	/// <param name="SwatchWidth">The width of one swatch.</param>
	/// <param name="SwatchSize">The height of one swatch.</param>
	/// <param name="Padding">The gap used between a swatch and its label, and between entries.</param>
	private readonly record struct LegendMetrics(
		double Width,
		double Height,
		double FontSize,
		double SwatchWidth,
		double SwatchSize,
		double Padding);
}
