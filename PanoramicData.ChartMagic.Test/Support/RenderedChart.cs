using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace PanoramicData.ChartMagic.Test.Support;

/// <summary>
/// Renders a chart to SVG and reads the tree back, so a suite can assert on what was drawn.
/// </summary>
/// <remarks>
/// Issue #28 is the failure mode where a chart that draws nothing passes its tests, so the suites
/// here assert on the rendered tree rather than on whether a file appeared. The reading is the
/// same wherever it is done and only the assertions differ, so it lives here once: every suite
/// pulls these in with <c>using static</c>.
/// </remarks>
internal static class RenderedChart
{
	/// <summary>
	/// The output width the suites render at. The geometry assertions are expressed against it,
	/// so it is shared rather than restated.
	/// </summary>
	internal const int Width = 800;

	/// <summary>The output height the suites render at.</summary>
	internal const int Height = 400;

	internal static XDocument Render(ChartSpecification specification)
		=> Render(specification.ToChart());

	internal static XDocument Render(ChartSpecification specification, int width, int height)
		=> Render(specification.ToChart(), width, height);

	internal static XDocument Render(Chart chart) => Render(chart, Width, Height);

	internal static XDocument Render(Chart chart, int width, int height)
	{
		using var stream = new MemoryStream();
		chart.SaveImage(stream, ChartImageFormat.Svg, width, height);
		return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
	}

	/// <summary>
	/// The group with this id, which the caller requires the chart to have drawn.
	/// </summary>
	internal static XElement GroupById(XDocument document, string id)
		=> FindGroupById(document, id)
			?? throw new InvalidOperationException($"The render contains no group with id '{id}'.");

	/// <summary>
	/// The group with this id, or null where the chart drew none - which is itself worth
	/// asserting for the parts of a chart that are not always present.
	/// </summary>
	internal static XElement? FindGroupById(XDocument document, string id)
		=> document
			.Descendants()
			.FirstOrDefault(e => e.Name.LocalName == "g" && e.Attribute("id")?.Value == id);

	/// <summary>
	/// Every descendant of the given element name, ignoring the SVG namespace.
	/// </summary>
	internal static List<XElement> Elements(XElement parent, string localName)
		=> [.. parent.Descendants().Where(e => e.Name.LocalName == localName)];

	/// <summary>
	/// A numeric attribute, parsed culture-independently as the SVG spells it.
	/// </summary>
	internal static double Number(XElement element, string name)
		=> double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

	/// <summary>
	/// The text of every label in a group, in document order.
	/// </summary>
	internal static List<string> LabelTexts(XDocument document, string groupId)
		=> [.. Elements(GroupById(document, groupId), "text").Select(t => t.Value)];

	/// <summary>
	/// The labels of a group read as numbers, which is what a value axis carries.
	/// </summary>
	internal static List<double> NumericLabels(XDocument document, string groupId)
		=> [.. LabelTexts(document, groupId).Select(v => double.Parse(v, CultureInfo.InvariantCulture))];

	/// <summary>
	/// The <c>defs</c> node, where the marker definitions live.
	/// </summary>
	internal static XElement Defs(XDocument document)
		=> document.Descendants().First(e => e.Name.LocalName == "defs");

	/// <summary>
	/// The X co-ordinates of the vertices of a path, in order.
	/// </summary>
	/// <remarks>
	/// A line series is one path of move-and-line commands rather than an element per point, so
	/// the point positions have to be read out of the geometry.
	/// </remarks>
	internal static List<double> PathVertexXValues(XElement path)
		=> [.. path
			.Attribute("d")!.Value
			.Split(['M', 'L'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(segment => segment.Split([',', ' '])[0])
			.Select(x => double.Parse(x, CultureInfo.InvariantCulture))];

	/// <summary>
	/// A group's translation, or the origin where it has none.
	/// </summary>
	internal static (double X, double Y) Translation(XElement group)
	{
		var transform = group.Attribute("transform")?.Value;
		if (transform is null)
		{
			return (0, 0);
		}

		var inside = transform[(transform.IndexOf('(') + 1)..transform.IndexOf(')')];
		var parts = inside.Split(',');
		return (
			double.Parse(parts[0], CultureInfo.InvariantCulture),
			double.Parse(parts[1], CultureInfo.InvariantCulture));
	}

	/// <summary>The X component of a group's translation.</summary>
	internal static double TranslationX(XElement group) => Translation(group).X;

	/// <summary>The Y component of a group's translation.</summary>
	internal static double TranslationY(XElement group) => Translation(group).Y;
}
