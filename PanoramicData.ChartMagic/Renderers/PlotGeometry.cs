namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// Maps data values onto pixel positions inside the inner plot, and answers the questions the
/// axis and series renderers both need to agree on.
/// </summary>
/// <remarks>
/// Issues #31 and #33: this mapping used to be computed inline in the series loop, so the axes
/// had no way to place a tick under the point it belongs to. Both now go through one object,
/// which is what keeps a category label beneath its column.
/// </remarks>
internal sealed class PlotGeometry
{
	private readonly double _xDisplayStart;
	private readonly double _xDisplayRange;
	private readonly double _yDisplayStart;
	private readonly double _yDisplayRange;
	private readonly double _yLogStart;
	private readonly double _yLogRange;
	private readonly List<double> _categories = [];
	private readonly Dictionary<double, double> _categoryTotals = [];
	private readonly Dictionary<double, string> _categoryLabels = [];

	internal PlotGeometry(Chart chart, AxisHandlerResult axisHandlerResult, double width, double height)
	{
		Width = width;
		Height = height;
		_xDisplayStart = chart.ChartArea.XAxis.Min ?? axisHandlerResult.MinX ?? 0;
		var xDisplayEnd = chart.ChartArea.XAxis.Max ?? axisHandlerResult.MaxX ?? 0;
		_xDisplayRange = xDisplayEnd - _xDisplayStart;
		XDisplayStart = _xDisplayStart;
		XDisplayEnd = xDisplayEnd;

		var bandedSeries = chart.Series.Where(series => IsBanded(series.ChartType)).ToList();
		IsCategorical = bandedSeries.Count > 0
			|| chart.Series.SelectMany(s => s.Points).Any(p => p.XValueString is { Length: > 0 });
		IsHorizontalPlot = bandedSeries.Count > 0
			&& bandedSeries.TrueForAll(series => IsHorizontal(series.ChartType));
		YIsLogarithmic = chart.ChartArea.YAxis.IsLogarithmic;
		PopulateCategories(chart);

		IsPercentStackedPlot = chart.Series.Any(series => IsPercentStacked(series.ChartType));
		if (IsPercentStackedPlot)
		{
			PopulateCategoryTotals(chart);
		}

		(ValueAxisInterval, _yDisplayStart, _yDisplayRange) = GetLinearAxis(chart, axisHandlerResult);
		(YLogMinimum, YLogMaximum, _yLogStart, _yLogRange) = GetLogarithmicAxis(chart);
	}

	private void PopulateCategories(Chart chart)
	{
		foreach (var point in chart.Series.SelectMany(series => series.Points))
		{
			if (!_categoryLabels.ContainsKey(point.XValue) && point.XValueString is not null)
			{
				_categoryLabels[point.XValue] = point.XValueString;
			}

			if (!_categories.Contains(point.XValue))
			{
				_categories.Add(point.XValue);
			}
		}

		_categories.Sort();
	}

	private void PopulateCategoryTotals(Chart chart)
	{
		foreach (var point in chart.Series
			.Where(series => IsPercentStacked(series.ChartType))
			.SelectMany(series => series.Points))
		{
			_categoryTotals[point.XValue] =
				_categoryTotals.GetValueOrDefault(point.XValue) + Math.Abs(point.YValue ?? 0);
		}
	}

	private (double? Interval, double Start, double Range) GetLinearAxis(
		Chart chart,
		AxisHandlerResult axisHandlerResult)
	{
		var yAxis = chart.ChartArea.YAxis;
		if (IsPercentStackedPlot)
		{
			return PercentStackedValueAxis(yAxis);
		}

		return YIsLogarithmic
			? LogarithmicValueAxis(yAxis, axisHandlerResult)
			: GeneratedValueAxis(yAxis, axisHandlerResult);
	}

	/// <summary>
	/// The value axis of a hundred per cent stacked plot, which is a percentage scale in fifths.
	/// </summary>
	private static (double? Interval, double Start, double Range) PercentStackedValueAxis(AxisArea yAxis)
	{
		var start = yAxis.Min ?? 0;
		return (20, start, (yAxis.Max ?? 100) - start);
	}

	/// <summary>
	/// The linear span of a logarithmic value axis, which is the data range as it stands: the
	/// decades themselves come from the logarithmic mapping rather than from an interval.
	/// </summary>
	private static (double? Interval, double Start, double Range) LogarithmicValueAxis(
		AxisArea yAxis,
		AxisHandlerResult axisHandlerResult)
	{
		var start = yAxis.Min ?? axisHandlerResult.MinY ?? 0;
		return (null, start, (yAxis.Max ?? axisHandlerResult.MaxY ?? 0) - start);
	}

	/// <summary>
	/// The value axis with bounds chosen to land on readable ticks, except at whichever end the
	/// chart pins for itself.
	/// </summary>
	private static (double? Interval, double Start, double Range) GeneratedValueAxis(
		AxisArea yAxis,
		AxisHandlerResult axisHandlerResult)
	{
		var (step, generatedStart, generatedEnd) = TickGenerator.LinearBounds(
			axisHandlerResult.MinY ?? 0,
			axisHandlerResult.MaxY ?? 0);
		var start = yAxis.Min ?? generatedStart;
		return (step, start, (yAxis.Max ?? generatedEnd) - start);
	}

	private static (double Minimum, double Maximum, double Start, double Range) GetLogarithmicAxis(Chart chart)
	{
		if (!chart.ChartArea.YAxis.IsLogarithmic)
		{
			return (0, 0, 0, 0);
		}

		var positiveValues = chart.Series.SelectMany(series => series.Points)
			.Where(point => point.YValue is > 0)
			.Select(point => point.YValue!.Value)
			.ToList();
		var smallest = chart.ChartArea.YAxis.Min is > 0
			? chart.ChartArea.YAxis.Min.Value
			: positiveValues.Count > 0 ? positiveValues.Min() : 1;
		var largest = chart.ChartArea.YAxis.Max is > 0
			? chart.ChartArea.YAxis.Max.Value
			: positiveValues.Count > 0 ? positiveValues.Max() : 10;
		var minimum = Math.Pow(10, Math.Floor(Math.Log10(smallest)));
		var maximum = Math.Pow(10, Math.Ceiling(Math.Log10(largest <= smallest ? smallest * 10 : largest)));
		var start = Math.Log10(minimum);
		return (minimum, maximum, start, Math.Log10(maximum) - start);
	}

	internal double Width { get; }

	internal double Height { get; }

	internal bool IsCategorical { get; }

	internal bool YIsLogarithmic { get; }

	/// <summary>Whether the category axis runs vertically, as it does for bar charts.</summary>
	internal bool IsHorizontalPlot { get; }

	/// <summary>
	/// The interval the value axis was laid out with, so the tick labels use the same one the
	/// bounds were derived from rather than choosing again from the adjusted range.
	/// </summary>
	internal double? ValueAxisInterval { get; private set; }

	internal double XDisplayStart { get; }

	internal double XDisplayEnd { get; }

	internal double YDisplayStart => YIsLogarithmic ? YLogMinimum : _yDisplayStart;

	internal double YDisplayEnd => YIsLogarithmic ? YLogMaximum : _yDisplayStart + _yDisplayRange;

	internal double YLogMinimum { get; }

	internal double YLogMaximum { get; }

	internal IReadOnlyList<double> Categories => _categories;

	/// <summary>
	/// Whether the plot shows shares of each category rather than amounts.
	/// </summary>
	internal bool IsPercentStackedPlot { get; private set; }

	/// <summary>
	/// A value as its percentage of the total for its category.
	/// </summary>
	/// <remarks>
	/// Returns nought for a category that sums to nothing, rather than dividing by it: an empty
	/// category has no shares to show, and a chart of infinities would be worse than a gap.
	/// </remarks>
	internal double ToPercentOfCategory(double xValue, double value)
	{
		var total = _categoryTotals.GetValueOrDefault(xValue);
		return IsNearlyZero(total) ? 0 : value / total * 100;
	}

	/// <summary>
	/// The number of intervals the category axis is divided into: one more than there are
	/// categories.
	/// </summary>
	/// <remarks>
	/// Not a rounding detail - it is where the categories sit. Dividing the axis by the number
	/// of categories and centring each in its share puts half an interval of padding at each
	/// end; dividing by one more and placing category i at interval i+1 puts a whole interval of
	/// padding at each end, which is what the renderer this matches does.
	///
	/// Measured rather than reasoned about: over an inner plot 488 pixels wide with seven
	/// categories, the other renderer spaced them 61 pixels apart starting 61 pixels in
	/// (488 / 8), where dividing by seven gives 70. The same 61 was measured for a column chart
	/// and for a line chart with markers, so it is a property of the category axis rather than of
	/// either chart type. Column groups were therefore drawn up to 27 pixels away from where they
	/// belonged, and proportionally wider with it.
	/// </remarks>
	private int CategoryIntervalCount => _categories.Count + 1;

	/// <summary>
	/// The width of one category interval, or zero when the axis is not categorical.
	/// </summary>
	internal double BandWidth => IsCategorical && _categories.Count > 0 ? Width / CategoryIntervalCount : 0;

	/// <summary>
	/// Whether this chart type occupies a band of the axis rather than a single position.
	/// </summary>
	internal static bool IsBanded(SeriesChartType chartType) => chartType
		is SeriesChartType.Column
		or SeriesChartType.StackedColumn
		or SeriesChartType.StackedColumn100
		or SeriesChartType.Bar
		or SeriesChartType.StackedBar
		or SeriesChartType.StackedBar100;

	/// <summary>
	/// Whether this chart type stacks onto the running total for its category.
	/// </summary>
	internal static bool IsStacked(SeriesChartType chartType) => chartType
		is SeriesChartType.StackedColumn
		or SeriesChartType.StackedColumn100
		or SeriesChartType.StackedBar
		or SeriesChartType.StackedBar100
		or SeriesChartType.StackedArea
		or SeriesChartType.StackedArea100;

	/// <summary>
	/// Whether this chart type stacks to a full hundred per cent, so the values are shares of
	/// their category rather than amounts.
	/// </summary>
	internal static bool IsPercentStacked(SeriesChartType chartType) => chartType
		is SeriesChartType.StackedColumn100
		or SeriesChartType.StackedBar100
		or SeriesChartType.StackedArea100;

	/// <summary>
	/// Whether this chart type runs along the Y axis rather than the X axis.
	/// </summary>
	internal static bool IsHorizontal(SeriesChartType chartType) => chartType
		is SeriesChartType.Bar
		or SeriesChartType.StackedBar
		or SeriesChartType.StackedBar100;

	/// <summary>
	/// The extent of one category band along the category axis - horizontal for columns,
	/// vertical for bars.
	/// </summary>
	internal double CategoryBandExtent => IsCategorical && _categories.Count > 0
		? (IsHorizontalPlot ? Height : Width) / CategoryIntervalCount
		: 0;

	/// <summary>
	/// The centre of a category along the category axis.
	/// </summary>
	/// <remarks>
	/// On a horizontal plot the category axis runs upwards: the first category is at the bottom
	/// and the last at the top, which is how the renderer this matches draws a bar chart, and the
	/// opposite of the order the categories are given in. Measured on a seven-day bar chart -
	/// reading the bar lengths off the reference render top to bottom gave Sunday first and Monday
	/// last. Drawing them in the order given put every category against the wrong label.
	/// </remarks>
	internal double CategoryToPixels(double xValue)
	{
		var index = _categories.IndexOf(xValue);
		if (index < 0)
		{
			index = 0;
		}

		var distanceAlongAxis = (index + 1) * CategoryBandExtent;

		// Distances are measured down the plot, so an axis that runs upwards is that distance
		// taken from the far edge.
		return Math.Round(
			IsHorizontalPlot ? Height - distanceAlongAxis : distanceAlongAxis,
			2);
	}

	/// <summary>
	/// The position of a value along the value axis: down the Y axis for columns, across the
	/// X axis for bars.
	/// </summary>
	internal double ValueToPixels(double yValue)
	{
		if (!IsHorizontalPlot)
		{
			return YToPixels(yValue);
		}

		return IsNearlyZero(_yDisplayRange)
			? 0
			: Math.Round(Width * (yValue - _yDisplayStart) / _yDisplayRange, 2);
	}

	/// <summary>
	/// Where zero sits on the value axis, clamped into the plot. Columns and bars are drawn
	/// from here, so when zero falls outside the plot they are drawn from the nearest edge.
	/// </summary>
	internal double ValueAxisOrigin => Math.Clamp(
		ValueToPixels(YIsLogarithmic ? YLogMinimum : 0),
		0,
		IsHorizontalPlot ? Width : Height);

	internal double XToPixels(double xValue)
	{
		if (IsCategorical)
		{
			var index = _categories.IndexOf(xValue);
			if (index < 0)
			{
				index = 0;
			}

			return Math.Round((index + 1) * BandWidth, 2);
		}

		return IsNearlyZero(_xDisplayRange)
			? 0
			: Math.Round(Width * (xValue - _xDisplayStart) / _xDisplayRange, 2);
	}

	internal double YToPixels(double yValue)
	{
		if (YIsLogarithmic)
		{
			if (IsNearlyZero(_yLogRange))
			{
				return Height;
			}

			// A non-positive value has no place on a logarithmic axis; pin it to the floor
			// rather than producing a NaN that silently removes the whole path.
			var clamped = yValue <= 0 ? YLogMinimum : yValue;
			return Math.Round(Height * (1 - ((Math.Log10(clamped) - _yLogStart) / _yLogRange)), 2);
		}

		return IsNearlyZero(_yDisplayRange)
			? Height
			: Math.Round(Height * (1 - ((yValue - _yDisplayStart) / _yDisplayRange)), 2);
	}

	/// <summary>
	/// The label for a category, when the data supplied one.
	/// </summary>
	internal string? CategoryLabel(double xValue)
		=> _categoryLabels.TryGetValue(xValue, out var label) ? label : null;

	private static bool IsNearlyZero(double value) => Math.Abs(value) < 1e-10;
}
