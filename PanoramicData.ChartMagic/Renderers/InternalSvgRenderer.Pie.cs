namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// Pies and doughnuts, which are drawn as a ring of wedges rather than against axes.
/// </summary>
internal partial class InternalSvgRenderer
{
	/// <summary>
	/// The offset that puts a pie start angle of zero at three o clock.
	/// </summary>
	private const double QuarterTurnDegrees = 90;

	/// <summary>
	/// Whether this chart type is drawn as a ring of slices rather than against axes.
	/// </summary>
	private static bool IsPie(Series series)
		=> series.ChartType is SeriesChartType.Pie or SeriesChartType.Doughnut;

	/// <summary>
	/// Draws a pie or doughnut: one wedge per slice, centred in the chart area, with the slice
	/// labels inside, outside on a leader line, or not at all.
	/// </summary>
	/// <remarks>
	/// Angles run clockwise from twelve o'clock, as they do in the Microsoft chart control, so a
	/// start angle of zero puts the first slice boundary at the top.
	/// </remarks>
	private void PlotPie(Series series, List<PieSlice> slices, XmlElement innerPlotNode, double plotWidth, double plotHeight)
	{
		if (slices.Count == 0)
		{
			return;
		}

		var pieNode = CreateGroup("pie");
		innerPlotNode.AppendChild(pieNode);

		// Centred in the inner plot, not the chart area. Measured against DocMagic: for a chart
		// area 468x400 with an inner plot inset 10% left and 10% vertically, its pie centre was
		// the inner plot centre and its diameter exactly 0.95 of the shorter inner plot side.
		// Drawing in the chart area instead put the pie 24px left and 20px high, and 8% small.
		var centreX = plotWidth / 2;
		var centreY = plotHeight / 2;

		// The pie is the same size whether its labels are inside or out. Shrinking it to make room
		// for outside labels seems the considerate thing to do, but the renderer this matches does
		// not: measured at 283 pixels across with outside labels and 285 with inside ones, where
		// shrinking gave 225. The labels are allowed to overflow instead, which is what the
		// reference render does - one of them sits outside the chart area entirely.
		var radius = Math.Min(plotWidth, plotHeight) / 2 * 0.95;

		// The percentage is the width of the RING, not the size of the hole, so the hole is what is
		// left over. This was the other way round, which inverted every doughnut: the default of 60
		// drew a 60% hole where the Microsoft chart control draws a 40% one, and asking for a thin
		// 30% ring gave a fat one.
		//
		// Measured on two doughnuts against the reference render, at 285 pixels across: the default
		// left a hole 116 wide (40.7%) and a specified 30 left one 202 wide (70.9%). Both are
		// 100 minus the percentage, and neither is the percentage itself.
		var innerRadius = series.ChartType == SeriesChartType.Doughnut
			? radius * (100 - Math.Clamp(series.DoughnutRadiusPercent ?? 60, 1, 100)) / 100
			: 0;

		foreach (var slice in slices)
		{
			pieNode.AppendChild(CreateWedge(series, slice, centreX, centreY, radius, innerRadius));
		}

		if (series.PieLabelStyle == PieLabelStyle.Disabled)
		{
			return;
		}

		PlotPieLabels(series, slices, pieNode, centreX, centreY, radius, innerRadius);
	}

	/// <summary>
	/// One wedge of a pie or doughnut, filled in the slice colour and outlined in the series one.
	/// </summary>
	private XmlElement CreateWedge(
		Series series,
		PieSlice slice,
		double centreX,
		double centreY,
		double radius,
		double innerRadius)
	{
		var wedge = _xmlDocument.CreateElement(string.Empty, "path", string.Empty);
		wedge.SetAttribute("d", WedgePath(centreX, centreY, radius, innerRadius, slice));
		wedge.SetAttribute("fill", slice.Color.ToHex());
		if (slice.Color.A != 255)
		{
			wedge.SetAttribute(
				"fill-opacity",
				(slice.Color.A / 255f).ToString("F2", CultureInfo.InvariantCulture));
		}

		if (series.StrokeColor != Colors.Transparent && series.StrokeWidth > 0)
		{
			wedge.SetAttribute("stroke", series.StrokeColor.ToHex());
			wedge.SetAttribute("stroke-width", series.StrokeWidth.ToString(CultureInfo.InvariantCulture));
		}

		return wedge;
	}

	/// <summary>
	/// The slice labels, drawn after every wedge so that a label is never covered by the next
	/// slice.
	/// </summary>
	private void PlotPieLabels(
		Series series,
		List<PieSlice> slices,
		XmlElement pieNode,
		double centreX,
		double centreY,
		double radius,
		double innerRadius)
	{
		// Labels drawn outside sit just clear of the edge; inside ones sit where the wedge is
		// widest.
		var labelsOutside = series.PieLabelStyle == PieLabelStyle.Outside;
		var labelStyle = TextStyle.Unstroked(series.FontWeight, series.FontFamily, series.FontSize, series.FontColor);

		foreach (var slice in slices.Where(s => s.Label.Length > 0))
		{
			var (at, alignment) = labelsOutside
				? OutsidePieLabelPosition(centreX, centreY, radius, slice)
				: InsidePieLabelPosition(centreX, centreY, radius, innerRadius, slice);

			pieNode.AppendChild(
				CreateTextNode(
					FormattableString.Invariant($"pieLabel{slice.StartAngleDegrees:F2}"),
					at.X,
					at.Y,
					slice.Label,
					alignment,
					VerticalAlignment.Middle,
					labelStyle));
		}
	}

	/// <summary>
	/// Where a label drawn outside the pie goes.
	/// </summary>
	/// <remarks>
	/// No leader line: the reference render draws none, and with the label sitting just clear of
	/// the edge there is nothing for one to bridge. The label is anchored away from the pie, so
	/// that the text runs outwards on both sides.
	/// </remarks>
	private static ((double X, double Y) At, HorizontalAlignment Alignment) OutsidePieLabelPosition(
		double centreX,
		double centreY,
		double radius,
		PieSlice slice)
	{
		var to = PointOnCircle(centreX, centreY, radius * 1.05, slice.MidAngleDegrees);
		var onTheRight = Math.Sin(ToRadians(slice.MidAngleDegrees)) >= 0;
		return (
			(to.X + (onTheRight ? 3 : -3), to.Y),
			onTheRight ? HorizontalAlignment.Left : HorizontalAlignment.Right);
	}

	/// <summary>
	/// Where a label drawn on the pie goes: midway through the ring for a doughnut, and two thirds
	/// out for a pie, which is where the wedge is widest.
	/// </summary>
	private static ((double X, double Y) At, HorizontalAlignment Alignment) InsidePieLabelPosition(
		double centreX,
		double centreY,
		double radius,
		double innerRadius,
		PieSlice slice)
	{
		var labelRadius = innerRadius > 0 ? (radius + innerRadius) / 2 : radius * 0.7;
		return (PointOnCircle(centreX, centreY, labelRadius, slice.MidAngleDegrees), HorizontalAlignment.Center);
	}

	/// <summary>
	/// The path for one wedge: a filled sector for a pie, or a band between two radii for a
	/// doughnut.
	/// </summary>
	private static string WedgePath(double centreX, double centreY, double radius, double innerRadius, PieSlice slice)
	{
		// A single slice covering the whole circle cannot be drawn as one arc, because its start
		// and end points coincide and the arc becomes a no-op. Two half arcs draw it correctly.
		var sweep = Math.Min(slice.SweepAngleDegrees, 360);
		if (sweep >= 359.999)
		{
			return FullRingPath(centreX, centreY, radius, innerRadius);
		}

		var start = slice.StartAngleDegrees;
		var end = start + sweep;
		var largeArc = sweep > 180 ? 1 : 0;

		var outerStart = PointOnCircle(centreX, centreY, radius, start);
		var outerEnd = PointOnCircle(centreX, centreY, radius, end);

		if (innerRadius <= 0)
		{
			return $"M{N(centreX)} {N(centreY)} L{N(outerStart.X)} {N(outerStart.Y)} "
				+ $"A{N(radius)} {N(radius)} 0 {largeArc} 1 {N(outerEnd.X)} {N(outerEnd.Y)} Z";
		}

		var innerEnd = PointOnCircle(centreX, centreY, innerRadius, end);
		var innerStart = PointOnCircle(centreX, centreY, innerRadius, start);

		return $"M{N(outerStart.X)} {N(outerStart.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 {largeArc} 1 {N(outerEnd.X)} {N(outerEnd.Y)} "
			+ $"L{N(innerEnd.X)} {N(innerEnd.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 {largeArc} 0 {N(innerStart.X)} {N(innerStart.Y)} Z";
	}

	private static string FullRingPath(double centreX, double centreY, double radius, double innerRadius)
	{
		var top = PointOnCircle(centreX, centreY, radius, 0);
		var bottom = PointOnCircle(centreX, centreY, radius, 180);
		var outer = $"M{N(top.X)} {N(top.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 1 1 {N(bottom.X)} {N(bottom.Y)} "
			+ $"A{N(radius)} {N(radius)} 0 1 1 {N(top.X)} {N(top.Y)} Z";

		if (innerRadius <= 0)
		{
			return outer;
		}

		// The hole is drawn the other way round, so that the default fill rule leaves it empty.
		var innerTop = PointOnCircle(centreX, centreY, innerRadius, 0);
		var innerBottom = PointOnCircle(centreX, centreY, innerRadius, 180);
		return outer
			+ $" M{N(innerTop.X)} {N(innerTop.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 1 0 {N(innerBottom.X)} {N(innerBottom.Y)} "
			+ $"A{N(innerRadius)} {N(innerRadius)} 0 1 0 {N(innerTop.X)} {N(innerTop.Y)} Z";
	}

	/// <summary>
	/// A point on a circle, at an angle measured clockwise from twelve o'clock.
	/// </summary>
	private static (double X, double Y) PointOnCircle(double centreX, double centreY, double radius, double angleDegrees)
	{
		// A start angle of zero puts the first slice boundary at three o clock, not twelve.
		// Established by rendering four equal quarters through both renderers: the Microsoft
		// chart control put the first quarter between three and six o clock.
		var radians = ToRadians(angleDegrees + QuarterTurnDegrees);
		return (centreX + (radius * Math.Sin(radians)), centreY - (radius * Math.Cos(radians)));
	}

	private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
