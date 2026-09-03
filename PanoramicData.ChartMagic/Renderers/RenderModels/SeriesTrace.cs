namespace PanoramicData.ChartMagic.Renderers.RenderModels;

/// <summary>
/// What one walk through a series' points produced: the paths, the return path a stacked fill
/// closes along, and the marker nodes.
/// </summary>
/// <param name="LinePath">The outline of the series, as an SVG path.</param>
/// <param name="AreaSegments">
/// The same run of points as path segments only, with no opening move. A fill is built by
/// prefixing a move to its baseline and suffixing the return path, because where the fill starts
/// and finishes is not known until the first and last points are.
/// </param>
/// <param name="FirstXPosition">
/// Where the first point sits horizontally, or null where the series had no points at all.
/// </param>
/// <param name="LastXPosition">Where the last point sits horizontally.</param>
/// <param name="ReturnPathPoints">
/// The points a stacked fill closes back along - the top of the series below it. Empty for a
/// series that is not stacked onto anything, which closes along its baseline instead.
/// </param>
/// <param name="MarkerNodes">
/// One marker reference per point, for a series that draws markers, and empty otherwise. They
/// are collected rather than appended as they are found so that the caller decides which group
/// they belong in.
/// </param>
internal sealed record SeriesTrace(
	string LinePath,
	string AreaSegments,
	double? FirstXPosition,
	double LastXPosition,
	List<(double X, double Y)> ReturnPathPoints,
	List<XmlElement> MarkerNodes);
