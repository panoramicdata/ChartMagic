namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// The axis strips and the gridlines that belong to them.
/// </summary>
internal partial class InternalSvgRenderer
{
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

		var gridNode = CreateGroup("gridlines");
		innerPlotNode.AppendChild(gridNode);

		PlotHorizontalGridlines(chart, geometry, yAxis, gridNode);
		PlotVerticalGridlines(chart, geometry, xAxis, gridNode);
	}

	/// <summary>
	/// The horizontal gridlines, which mark the values on the Y axis.
	/// </summary>
	/// <remarks>
	/// Minor lines before major, so a major line is drawn over a coincident minor one.
	/// </remarks>
	private void PlotHorizontalGridlines(Chart chart, PlotGeometry geometry, AxisArea yAxis, XmlElement gridNode)
	{
		if (yAxis.MinorGridEnabled)
		{
			foreach (var value in MinorTicks(yAxis, geometry, isValueAxis: !geometry.IsHorizontalPlot))
			{
				var y = geometry.YToPixels(value);
				gridNode.AppendChild(CreateLine(0, y, geometry.Width, y, yAxis.MinorGridColor, yAxis.GridWidth));
			}
		}

		if (!yAxis.MajorGridEnabled)
		{
			return;
		}

		foreach (var value in YAxisTickValues(chart, geometry))
		{
			var y = geometry.IsHorizontalPlot ? geometry.CategoryToPixels(value) : geometry.YToPixels(value);
			gridNode.AppendChild(CreateLine(0, y, geometry.Width, y, yAxis.MajorGridColor, yAxis.GridWidth));
		}
	}

	/// <summary>
	/// The vertical gridlines, which mark the values on the X axis.
	/// </summary>
	private void PlotVerticalGridlines(Chart chart, PlotGeometry geometry, AxisArea xAxis, XmlElement gridNode)
	{
		if (xAxis.MinorGridEnabled)
		{
			foreach (var x in MinorGridPositions(xAxis, geometry))
			{
				gridNode.AppendChild(CreateLine(x, 0, x, geometry.Height, xAxis.MinorGridColor, xAxis.GridWidth));
			}
		}

		if (!xAxis.MajorGridEnabled)
		{
			return;
		}

		foreach (var value in XAxisTickValues(chart, geometry))
		{
			var x = XAxisPixels(geometry, value);
			gridNode.AppendChild(CreateLine(x, 0, x, geometry.Height, xAxis.MajorGridColor, xAxis.GridWidth));
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
		var labelStyle = TextStyle.Unstroked(xAxis.FontWeight, xAxis.FontFamily, xAxis.FontSize, xAxis.FontColor);

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
					labelStyle,
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
					labelStyle with { FontWeight = FontWeight.Bold }));
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
		var labelStyle = TextStyle.Unstroked(yAxis.FontWeight, yAxis.FontFamily, yAxis.FontSize, yAxis.FontColor);

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
					labelStyle,
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
					labelStyle with { FontWeight = FontWeight.Bold },
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

		return TickGenerator.Linear(
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

		return CategoryBandSubdivisions(axis, geometry);
	}

	/// <summary>
	/// Subdivisions of each category band, measured from the band edge rather than its centre, so
	/// the lines fall between the categories as well as within them.
	/// </summary>
	private static IReadOnlyList<double> CategoryBandSubdivisions(AxisArea axis, PlotGeometry geometry)
	{
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
			return ShortAxisLabel(value);
		}

		// Two decimal places at most, and none where the value does not need them.
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// A short axis label: one decimal place, with a suffix once the value reaches a thousand.
	/// </summary>
	/// <remarks>
	/// Measured against DocMagic: with short labels on and values topping out at 35, its axis
	/// reads 35.0, 30.0, 25.0 rather than 35, 30, 25. So "short" is not only about abbreviating
	/// large numbers - it is a fixed one-decimal format throughout, and this implementation left
	/// anything under a thousand alone, which is why the setting had no effect on a percentage
	/// axis.
	/// </remarks>
	private static string ShortAxisLabel(double value)
	{
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
}
