namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// The point markers: one shape definition per series, placed at each point by translation.
/// </summary>
internal partial class InternalSvgRenderer
{
	/// <summary>
	/// Builds the marker shape for a series, centred on the origin so that a single definition can
	/// be placed at each point by translation alone.
	/// </summary>
	/// <remarks>
	/// Issue #30: only Circle was implemented, and every other value of the enum threw
	/// NotSupportedException - so a chart asking for square markers failed outright rather than
	/// rendering. The sizes follow the Microsoft chart control convention that marker size is the
	/// full width of the marker, not its radius.
	/// </remarks>
	private XmlElement? CreateMarkerDefinition(Series series, string id)
	{
		if (series.MarkerStyle == MarkerStyle.None)
		{
			return null;
		}

		// The Microsoft control treats marker size as the overall width; a circle takes half of
		// it as its radius, which is what the original Circle case did with StrokeWidth.
		var node = CreateMarkerShape(series.MarkerStyle, series.MarkerSize ?? series.StrokeWidth * 2);

		node.SetAttribute("id", id);
		node.SetAttribute("fill", (series.MarkerFillColor ?? series.FillColor).ToHex());
		node.SetAttribute("stroke", (series.MarkerStrokeColor ?? series.StrokeColor).ToHex());
		node.SetAttribute(
			"stroke-width",
			(series.MarkerStrokeWidth ?? series.StrokeWidth).ToString(CultureInfo.InvariantCulture));
		return node;
	}

	/// <summary>
	/// A reference to a series' marker definition, placed at one point.
	/// </summary>
	private XmlElement CreateMarkerReference(string markerId, double x, double y)
	{
		var markerNode = _xmlDocument.CreateElement(string.Empty, "use", string.Empty);
		markerNode.SetAttribute("xlink:href", $"#{markerId}");
		markerNode.SetAttribute("transform", $"translate({x} {y})");
		return markerNode;
	}

	/// <summary>
	/// The unstyled shape for a marker style, centred on the origin.
	/// </summary>
	/// <param name="markerStyle">The shape asked for.</param>
	/// <param name="size">The overall width of the marker.</param>
	private XmlElement CreateMarkerShape(MarkerStyle markerStyle, double size)
	{
		var half = size / 2;
		return markerStyle switch
		{
			MarkerStyle.Circle => Circle(half),
			MarkerStyle.Square => Square(size, half),
			MarkerStyle.Diamond => Polygon([(0, -half), (half, 0), (0, half), (-half, 0)]),
			// A triangle is centred on its centroid rather than its bounding box, so it sits on
			// the point the way the other shapes do.
			MarkerStyle.Triangle => Polygon([(0, -half), (half, half * 0.6), (-half, half * 0.6)]),
			MarkerStyle.Cross => Cross(half),
			MarkerStyle.Star4 => Star(4, half),
			MarkerStyle.Star5 => Star(5, half),
			MarkerStyle.Star6 => Star(6, half),
			MarkerStyle.Star10 => Star(10, half),
			_ => throw new NotSupportedException($"Marker type {markerStyle} is not supported.")
		};
	}

	private XmlElement Circle(double radius)
	{
		var node = _xmlDocument.CreateElement(string.Empty, "circle", string.Empty);
		node.SetAttribute("r", N(radius));
		return node;
	}

	private XmlElement Square(double size, double half)
	{
		var node = _xmlDocument.CreateElement(string.Empty, "rect", string.Empty);
		node.SetAttribute("x", N(-half));
		node.SetAttribute("y", N(-half));
		node.SetAttribute("width", N(size));
		node.SetAttribute("height", N(size));
		return node;
	}

	/// <summary>
	/// A plus sign of the same width as the other markers, one third of it thick.
	/// </summary>
	private XmlElement Cross(double half)
	{
		var arm = half / 3;
		return Polygon(
		[
			(-arm, -half), (arm, -half), (arm, -arm), (half, -arm),
			(half, arm), (arm, arm), (arm, half), (-arm, half),
			(-arm, arm), (-half, arm), (-half, -arm), (-arm, -arm)
		]);
	}

	/// <summary>
	/// A closed polygon through the given points, which are relative to the marker centre.
	/// </summary>
	private XmlElement Polygon(IReadOnlyList<(double X, double Y)> points)
	{
		var node = _xmlDocument.CreateElement(string.Empty, "polygon", string.Empty);
		node.SetAttribute("points", string.Join(" ", points.Select(p => $"{N(p.X)},{N(p.Y)}")));
		return node;
	}

	/// <summary>
	/// A star with the given number of points, alternating between the outer radius and an inner
	/// one at 40% of it.
	/// </summary>
	private XmlElement Star(int points, double outerRadius)
	{
		var innerRadius = outerRadius * 0.4;
		var vertices = new List<(double X, double Y)>();

		for (var index = 0; index < points * 2; index++)
		{
			// Starting at twelve o'clock so a five-pointed star sits upright.
			var angle = (index * Math.PI / points) - (Math.PI / 2);
			var radius = index % 2 == 0 ? outerRadius : innerRadius;
			vertices.Add((radius * Math.Cos(angle), radius * Math.Sin(angle)));
		}

		return Polygon(vertices);
	}
}
