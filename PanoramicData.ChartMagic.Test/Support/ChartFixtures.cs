using System.Drawing;

namespace PanoramicData.ChartMagic.Test.Support;

/// <summary>
/// The chart specifications the rendering suites assert against.
/// </summary>
/// <remarks>
/// Shared rather than built per suite, so that a test reads as the one thing it varies. The
/// sample values are the ones the axis assertions are expressed in terms of - a minimum of 10 and
/// a maximum of 31 - so changing them here changes what those tests mean.
/// </remarks>
internal static class ChartFixtures
{
	/// <summary>The categories the sample data is labelled with.</summary>
	internal static readonly string[] Categories = ["Jan", "Feb", "Mar", "Apr"];

	/// <summary>The palette the sample series are coloured from.</summary>
	private static readonly Color[] Palette = [Color.SteelBlue, Color.SeaGreen, Color.Goldenrod];

	/// <summary>
	/// Points labelled with the sample categories, one per value.
	/// </summary>
	internal static List<ChartPoint> Points(params double[] values)
		=> [.. values.Select((value, index) => new ChartPoint(Categories[index], index, value))];

	/// <summary>
	/// A chart of the given type with the given number of series, over the sample categories.
	/// </summary>
	internal static ChartSpecification ColumnChart(SeriesChartType chartType, int seriesCount)
		=> new()
		{
			SeriesList =
			[
				.. Enumerable.Range(0, seriesCount).Select(index => new SeriesSpecification
				{
					ChartType = chartType,
					FillColor = Palette[index % Palette.Length],
					StrokeColor = Palette[index % Palette.Length],
					Points = Points(10 + (index * 4), 24 - (index * 3), 17 + index, 31 - (index * 5)),
				})
			]
		};

	/// <summary>
	/// A single-series chart of the given type over the points supplied. Only the stroke colour
	/// is set, because a fill affects what a marker or an area is drawn in and so belongs with
	/// the test that cares about it.
	/// </summary>
	internal static ChartSpecification SingleSeries(
		SeriesChartType chartType,
		List<ChartPoint> points,
		Action<SeriesSpecification>? configureSeries = null)
	{
		var series = new SeriesSpecification
		{
			ChartType = chartType,
			StrokeColor = Palette[0],
			Points = points,
		};

		configureSeries?.Invoke(series);
		return new ChartSpecification { SeriesList = [series] };
	}
}
