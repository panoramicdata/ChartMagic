using System.Drawing;

namespace PanoramicData.ChartMagic.Renderers.RenderModels;

/// <summary>
/// How a piece of text is drawn: the face, the size, and the two colours.
/// </summary>
/// <param name="FontWeight">The weight the text is drawn at.</param>
/// <param name="FontFamily">
/// The font family, or null to fall back to the renderer's default stack.
/// </param>
/// <param name="FontSize">The font size, in pixels.</param>
/// <param name="StrokeColor">
/// The outline colour. Transparent - which is the usual case for a label - means no outline is
/// written at all, because alpha is discarded when a colour is written out.
/// </param>
/// <param name="FillColor">The colour the glyphs are filled with.</param>
/// <remarks>
/// Grouped into one value because the five travel together everywhere: every caller reads them
/// off a single element - an axis, a legend, a series or an annotation - and passes them straight
/// through unchanged.
/// </remarks>
internal readonly record struct TextStyle(
	FontWeight FontWeight,
	string? FontFamily,
	double FontSize,
	Color StrokeColor,
	Color FillColor)
{
	/// <summary>
	/// The style for a label drawn from an element's font settings, with no outline.
	/// </summary>
	internal static TextStyle Unstroked(
		FontWeight fontWeight,
		string? fontFamily,
		double fontSize,
		Color fillColor)
		=> new(fontWeight, fontFamily, fontSize, Colors.Transparent, fillColor);
}
