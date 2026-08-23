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
	private double _yDisplayStart;
	private double _yDisplayRange;
	private readonly double _yLogStart;
	private readonly double _yLogRange;
	private readonly List<double> _categories = [];
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

		_yDisplayStart = chart.ChartArea.YAxis.Min ?? axisHandlerResult.MinY ?? 0;
		var yDisplayEnd = chart.ChartArea.YAxis.Max ?? axisHandlerResult.MaxY ?? 0;
		_yDisplayRange = yDisplayEnd - _yDisplayStart;

		// A column or bar occupies a band rather than a point, so a plot containing one is
		// laid out by category: N categories divide the width evenly and each is drawn at the
		// centre of its band. Any line or area series in the same plot follows the same
		// mapping, which is what keeps them aligned with the columns.
		var bandedSeries = chart.Series.Where(s => IsBanded(s.ChartType)).ToList();

		// The axis is categorical if a column or bar needs a band to stand in, and also if the
		// data supplied labels rather than numbers. Without the second case a line chart over
		// seven named days was labelled at generated numeric intervals instead, which showed
		// every other day and dropped the rest.
		IsCategorical = bandedSeries.Count > 0
			|| chart.Series.SelectMany(s => s.Points).Any(p => p.XValueString is { Length: > 0 });

		// A plot is horizontal only when every banded series in it is. Mixing bars and columns
		// in one plot has no single sensible orientation, so it falls back to vertical.
		// Count first: TrueForAll is vacuously true on an empty list, so without it a line
		// chart over labelled categories - which has no banded series at all - was treated as
		// horizontal and rendered with its axes swapped.
		IsHorizontalPlot = bandedSeries.Count > 0 && bandedSeries.TrueForAll(s => IsHorizontal(s.ChartType));

		// The categories, in the order they will be laid out, and their labels.
		foreach (var point in chart.Series.SelectMany(s => s.Points))
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

		// The displayed value range, matching what the Microsoft chart control chooses.
		//
		// Measured against DocMagic across four data sets: its value axis is zero-based and runs
		// to the next multiple of the interval strictly above the data maximum. A peak of 30
		// gave an axis to 35, a peak of 32 also gave 35, and a peak of 280 gave 300 - so it is
		// not plain rounding up to the next tick, which would have left the first at 30 with the
		// tallest column touching the frame.
		//
		// This applies to every chart type, not only columns. The line case looked close on a
		// pixel count purely because a thin line covers few pixels: its axis ran 11.5 to 30.75
		// against DocMagic 0 to 35, which is not close at all.
		if (!YIsLogarithmic)
		{
			var dataMinimum = axisHandlerResult.MinY ?? 0;
			var dataMaximum = axisHandlerResult.MaxY ?? 0;
			var provisionalRange = Math.Max(Math.Abs(dataMaximum), Math.Abs(dataMinimum));
			ValueAxisInterval = TickGenerator.NiceStep(
				provisionalRange > 0 ? provisionalRange : 1,
				chart.ChartArea.YAxis.TargetTickCount);

			if (chart.ChartArea.YAxis.Min is null)
			{
				// Zero-based unless the data goes below zero, in which case the axis extends down
				// to a whole interval instead.
				_yDisplayStart = dataMinimum >= 0
					? 0
					: -TickGenerator.NextStepAbove(-dataMinimum, ValueAxisInterval.Value);
			}

			var end = chart.ChartArea.YAxis.Max
				?? TickGenerator.NextStepAbove(dataMaximum, ValueAxisInterval.Value);
			_yDisplayRange = end - _yDisplayStart;
		}
		// Logarithmic bounds are snapped out to whole decades, so the axis reads
		// 1, 10, 100 rather than starting partway up a decade.
		YIsLogarithmic = chart.ChartArea.YAxis.IsLogarithmic;
		if (YIsLogarithmic)
		{
			var positiveValues = chart.Series
				.SelectMany(s => s.Points)
				.Where(p => p.YValue is > 0)
				.Select(p => p.YValue!.Value)
				.ToList();

			var smallest = chart.ChartArea.YAxis.Min is > 0
				? chart.ChartArea.YAxis.Min!.Value
				: positiveValues.Count > 0 ? positiveValues.Min() : 1;
			var largest = chart.ChartArea.YAxis.Max is > 0
				? chart.ChartArea.YAxis.Max!.Value
				: positiveValues.Count > 0 ? positiveValues.Max() : 10;

			YLogMinimum = Math.Pow(10, Math.Floor(Math.Log10(smallest)));
			YLogMaximum = Math.Pow(10, Math.Ceiling(Math.Log10(largest <= smallest ? smallest * 10 : largest)));
			_yLogStart = Math.Log10(YLogMinimum);
			_yLogRange = Math.Log10(YLogMaximum) - _yLogStart;
		}
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
	/// The width of one category band, or zero when the axis is not categorical.
	/// </summary>
	internal double BandWidth => IsCategorical && _categories.Count > 0 ? Width / _categories.Count : 0;

	/// <summary>
	/// Whether this chart type occupies a band of the axis rather than a single position.
	/// </summary>
	internal static bool IsBanded(SeriesChartType chartType) => chartType
		is SeriesChartType.Column
		or SeriesChartType.StackedColumn
		or SeriesChartType.Bar
		or SeriesChartType.StackedBar;

	/// <summary>
	/// Whether this chart type stacks onto the running total for its category.
	/// </summary>
	internal static bool IsStacked(SeriesChartType chartType) => chartType
		is SeriesChartType.StackedColumn
		or SeriesChartType.StackedBar
		or SeriesChartType.StackedArea;

	/// <summary>
	/// Whether this chart type runs along the Y axis rather than the X axis.
	/// </summary>
	internal static bool IsHorizontal(SeriesChartType chartType) => chartType
		is SeriesChartType.Bar
		or SeriesChartType.StackedBar;

	/// <summary>
	/// The extent of one category band along the category axis - horizontal for columns,
	/// vertical for bars.
	/// </summary>
	internal double CategoryBandExtent => IsCategorical && _categories.Count > 0
		? (IsHorizontalPlot ? Height : Width) / _categories.Count
		: 0;

	/// <summary>
	/// The centre of a category along the category axis.
	/// </summary>
	internal double CategoryToPixels(double xValue)
	{
		var index = _categories.IndexOf(xValue);
		if (index < 0)
		{
			index = 0;
		}

		return Math.Round((index + 0.5) * CategoryBandExtent, 2);
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

		return _yDisplayRange == 0
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

			return Math.Round((index + 0.5) * BandWidth, 2);
		}

		return _xDisplayRange == 0
			? 0
			: Math.Round(Width * (xValue - _xDisplayStart) / _xDisplayRange, 2);
	}

	internal double YToPixels(double yValue)
	{
		if (YIsLogarithmic)
		{
			if (_yLogRange == 0)
			{
				return Height;
			}

			// A non-positive value has no place on a logarithmic axis; pin it to the floor
			// rather than producing a NaN that silently removes the whole path.
			var clamped = yValue <= 0 ? YLogMinimum : yValue;
			return Math.Round(Height * (1 - ((Math.Log10(clamped) - _yLogStart) / _yLogRange)), 2);
		}

		return _yDisplayRange == 0
			? Height
			: Math.Round(Height * (1 - ((yValue - _yDisplayStart) / _yDisplayRange)), 2);
	}

	/// <summary>
	/// The label for a category, when the data supplied one.
	/// </summary>
	internal string? CategoryLabel(double xValue)
		=> _categoryLabels.TryGetValue(xValue, out var label) ? label : null;
}
