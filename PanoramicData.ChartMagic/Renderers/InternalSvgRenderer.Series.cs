namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// The series themselves: lines, filled areas, and columns or bars.
/// </summary>
internal partial class InternalSvgRenderer
{
	private void PlotSeries(Chart chart, PlotGeometry geometry, XmlElement defs, XmlElement innerPlotNode)
	{
		var stackedColumnTotals = new Dictionary<string, double>();
		var stackedAreaTotals = new Dictionary<string, double>();
		var stackLines = CreateGroup("stackLines");
		var bands = BandLayout.For(chart);

		var seriesIndex = -1;
		foreach (var series in chart.Series)
		{
			var seriesNode = CreateGroup($"series{++seriesIndex}");

			// Add markers to defs if required
			var seriesMarkerId = $"series{seriesIndex}Marker";
			var markerDefinition = CreateMarkerDefinition(series, seriesMarkerId);
			if (markerDefinition is not null)
			{
				defs.AppendChild(markerDefinition);
			}

			var stackTotals = StackTotalsFor(series.ChartType, stackedColumnTotals, stackedAreaTotals);

			if (PlotGeometry.IsBanded(series.ChartType))
			{
				PlotBandedSeries(chart, geometry, series, seriesNode, stackTotals, bands.SlotFor(series), bands.SlotCount);
			}
			else
			{
				PlotPointSeries(geometry, series, seriesNode, stackLines, stackTotals, seriesMarkerId);
			}

			innerPlotNode.AppendChild(seriesNode);
		}

		if (stackLines.ChildNodes.Count != 0)
		{
			innerPlotNode.AppendChild(stackLines);
		}
	}

	/// <summary>
	/// The running totals a series stacks onto, or null where it does not stack.
	/// </summary>
	/// <remarks>
	/// Columns and bars share one set of totals because a chart mixes neither with the other, and
	/// areas keep their own so that a stacked area drawn alongside stacked columns does not stack
	/// onto them.
	/// </remarks>
	private static Dictionary<string, double>? StackTotalsFor(
		SeriesChartType chartType,
		Dictionary<string, double> bandTotals,
		Dictionary<string, double> areaTotals)
		=> chartType switch
		{
			SeriesChartType.StackedColumn or SeriesChartType.StackedColumn100 => bandTotals,
			SeriesChartType.StackedBar or SeriesChartType.StackedBar100 => bandTotals,
			SeriesChartType.StackedArea or SeriesChartType.StackedArea100 => areaTotals,
			_ => null
		};

	/// <summary>
	/// Draws a series that is a run of points: its fill where it has one, then its line and any
	/// markers.
	/// </summary>
	/// <remarks>
	/// A stacked area draws its fill in its own group and its line in the shared one, so that
	/// every line is drawn over every fill rather than being buried by the next series.
	/// </remarks>
	private void PlotPointSeries(
		PlotGeometry geometry,
		Series series,
		XmlElement seriesNode,
		XmlElement stackLines,
		Dictionary<string, double>? stackTotals,
		string markerId)
	{
		var trace = TracePoints(geometry, series, stackTotals, markerId);

		switch (series.ChartType)
		{
			case SeriesChartType.Area:
				seriesNode.AppendChild(CreateAreaNode(geometry, series, trace));
				AppendLinePath(seriesNode, series, trace);
				break;

			case SeriesChartType.StackedArea:
			case SeriesChartType.StackedArea100:
				seriesNode.AppendChild(CreateAreaNode(geometry, series, trace));
				AppendLinePath(stackLines, series, trace);
				break;

			case SeriesChartType.Line:
			case SeriesChartType.FastLine:
				AppendLinePath(seriesNode, series, trace);
				break;
		}
	}

	/// <summary>
	/// Walks a series' points once, building the paths, the stack return path and the markers.
	/// </summary>
	private SeriesTrace TracePoints(
		PlotGeometry geometry,
		Series series,
		Dictionary<string, double>? stackTotals,
		string markerId)
	{
		var linePath = new StringBuilder();
		// The outline only. Where the fill starts and finishes is decided once the first and last
		// points are known, because it belongs under them and not at the edges of the plot.
		var areaSegments = new StringBuilder();
		double? firstXPosition = null;
		var lastXPosition = 0d;
		var returnPathPoints = new List<(double X, double Y)>();
		var markerNodes = new List<XmlElement>();
		var isFirstPoint = true;

		foreach (var chartPoint in series.Points)
		{
			var yValue = StackedValue(geometry, chartPoint, stackTotals, out var previousYValue);

			var xPosition = geometry.IsCategorical
				? geometry.CategoryToPixels(chartPoint.XValue)
				: geometry.XToPixels(chartPoint.XValue);
			var yPosition = geometry.YToPixels(yValue);
			if (previousYValue is not null)
			{
				returnPathPoints.Add((xPosition, geometry.YToPixels(previousYValue.Value)));
			}

			// Letter - always M to start, afterwards L unless the previous value is null
			linePath.Append($"{(isFirstPoint ? "M" : " L")}{xPosition} {yPosition}");
			areaSegments.Append($" L{xPosition} {yPosition}");
			firstXPosition ??= xPosition;
			lastXPosition = xPosition;
			isFirstPoint = false;

			if (series.MarkerStyle != MarkerStyle.None)
			{
				markerNodes.Add(CreateMarkerReference(markerId, xPosition, yPosition));
			}
		}

		return new SeriesTrace(
			linePath.ToString(),
			areaSegments.ToString(),
			firstXPosition,
			lastXPosition,
			returnPathPoints,
			markerNodes);
	}

	/// <summary>
	/// The value a point is drawn at, which for a stacked series is its contribution added to the
	/// running total for its category.
	/// </summary>
	/// <param name="geometry">The plot the point is being drawn into.</param>
	/// <param name="chartPoint">The point.</param>
	/// <param name="stackTotals">
	/// The running totals for the series that stack with this one, or null where it does not stack.
	/// </param>
	/// <param name="previousTotal">
	/// The total this point stacks onto, or null where it is not stacked onto anything. A point
	/// with no value still reports one, because the series below it still has to be closed off.
	/// </param>
	private static double StackedValue(
		PlotGeometry geometry,
		ChartPoint chartPoint,
		Dictionary<string, double>? stackTotals,
		out double? previousTotal)
	{
		previousTotal = null;
		if (stackTotals is null)
		{
			return chartPoint.YValue ?? 0;
		}

		var key = chartPoint.XValue.ToString(CultureInfo.InvariantCulture);
		if (stackTotals.TryGetValue(key, out var runningTotal))
		{
			previousTotal = runningTotal;
		}

		if (chartPoint.YValue is null)
		{
			return 0;
		}

		// A hundred per cent stacked series contributes its share of the category, not its value.
		var contribution = geometry.IsPercentStackedPlot
			? geometry.ToPercentOfCategory(chartPoint.XValue, chartPoint.YValue.Value)
			: chartPoint.YValue.Value;

		var total = contribution + (previousTotal ?? 0);
		stackTotals[key] = total;
		return total;
	}

	/// <summary>
	/// The filled area beneath a traced series.
	/// </summary>
	/// <remarks>
	/// The fill hangs below the line it follows, from the first point to the last. It used to
	/// start at the bottom-left corner of the plot and finish at the bottom-right, which drew a
	/// diagonal ramp up to the first point and another down from the last - inventing data on
	/// either side of the series. With a whole category interval of padding at each end of the
	/// axis, those ramps were a sixth of the chart wide.
	///
	/// And it hangs to the zero line, not to the floor of the plot, so a series with negative
	/// values fills downwards from zero rather than upwards from the bottom.
	/// </remarks>
	private XmlElement CreateAreaNode(PlotGeometry geometry, Series series, SeriesTrace trace)
	{
		var baseline = geometry.ValueAxisOrigin;
		var returnPathPoints = trace.ReturnPathPoints;
		if (returnPathPoints.Count == 0)
		{
			returnPathPoints = [(trace.LastXPosition, baseline)];
		}

		var areaPath = new StringBuilder(
			FormattableString.Invariant($"M{trace.FirstXPosition ?? 0} {baseline}"));
		areaPath.Append(trace.AreaSegments);
		areaPath.Append(string.Join("", returnPathPoints.AsEnumerable().Reverse().Select(p => $"L{p.X} {p.Y}")));
		areaPath.Append('Z');

		var areaNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
		areaNode.SetAttribute("d", areaPath.ToString());
		areaNode.SetStyle(series, applyStroke: false);
		return areaNode;
	}

	/// <summary>
	/// Appends a traced series' line, and its markers behind it, to a group.
	/// </summary>
	private void AppendLinePath(XmlElement target, Series series, SeriesTrace trace)
	{
		var pathNode = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
		pathNode.SetAttribute("d", trace.LinePath);
		pathNode.SetStyle(series, applyFill: false);
		target.AppendChild(pathNode);

		foreach (var markerNode in trace.MarkerNodes)
		{
			target.AppendChild(markerNode);
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
		Dictionary<string, double>? stackTotals,
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

			var (from, to) = BandSpan(geometry, chartPoint, stackTotals, origin);
			var slotStart = geometry.CategoryToPixels(chartPoint.XValue) - (groupExtent / 2) + (slot * slotExtent);

			var rectNode = CreateBandRect(isHorizontal, from, to, slotStart, slotExtent);
			rectNode.SetStyle(series);
			seriesNode.AppendChild(rectNode);
		}
	}

	/// <summary>
	/// Where one column or bar starts and finishes along the value axis, in pixels.
	/// </summary>
	private static (double From, double To) BandSpan(
		PlotGeometry geometry,
		ChartPoint chartPoint,
		Dictionary<string, double>? stackTotals,
		double origin)
	{
		if (stackTotals is null)
		{
			return (origin, geometry.ValueToPixels(chartPoint.YValue!.Value));
		}

		var key = chartPoint.XValue.ToString(CultureInfo.InvariantCulture);
		var previousTotal = stackTotals.TryGetValue(key, out var runningTotal) ? runningTotal : 0;

		// A hundred per cent stacked series contributes its share of the category, not its value.
		var contribution = geometry.IsPercentStackedPlot
			? geometry.ToPercentOfCategory(chartPoint.XValue, chartPoint.YValue!.Value)
			: chartPoint.YValue!.Value;

		var newTotal = previousTotal + contribution;
		stackTotals[key] = newTotal;
		return (geometry.ValueToPixels(previousTotal), geometry.ValueToPixels(newTotal));
	}

	/// <summary>
	/// The rectangle for one column or bar: its span lies along the value axis and its thickness
	/// across the category axis, whichever way round those two are.
	/// </summary>
	private XmlElement CreateBandRect(bool isHorizontal, double from, double to, double slotStart, double slotExtent)
	{
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

		return rectNode;
	}

	/// <summary>
	/// How the column and bar series of a chart divide up a category band.
	/// </summary>
	/// <param name="GroupedSeries">The banded series that stand side by side.</param>
	/// <param name="SlotCount">How many slots a band is divided into.</param>
	/// <remarks>
	/// Issue #33: a column or bar occupies a slot within its category band. Grouped series take
	/// one slot each; all stacked series share a single slot, because they stack on top of one
	/// another rather than standing side by side.
	/// </remarks>
	private sealed record BandLayout(List<Series> GroupedSeries, int SlotCount)
	{
		internal static BandLayout For(Chart chart)
		{
			var bandedSeries = chart.Series.Where(s => PlotGeometry.IsBanded(s.ChartType)).ToList();
			var groupedSeries = bandedSeries.Where(s => !PlotGeometry.IsStacked(s.ChartType)).ToList();
			var hasStackedBanded = bandedSeries.Exists(s => PlotGeometry.IsStacked(s.ChartType));
			return new BandLayout(groupedSeries, Math.Max(1, groupedSeries.Count + (hasStackedBanded ? 1 : 0)));
		}

		/// <summary>
		/// The slot a series occupies: its own, or the shared last one if it is stacked.
		/// </summary>
		internal int SlotFor(Series series)
			=> PlotGeometry.IsStacked(series.ChartType)
				? GroupedSeries.Count
				: GroupedSeries.IndexOf(series);
	}
}
