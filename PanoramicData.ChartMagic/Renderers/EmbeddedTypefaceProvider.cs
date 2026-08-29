using SkiaSharp;
using Svg.Skia.TypefaceProviders;
using System.Reflection;

namespace PanoramicData.ChartMagic.Renderers;

/// <summary>
/// Supplies the font ChartMagic draws text with, from a copy embedded in this assembly.
/// </summary>
/// <remarks>
/// Issue #60. Without this, charts rendered on Linux contained no text at all - no axis labels,
/// no tick values, no legend text, no titles - while the bars, lines and colours were correct.
///
/// The cause is not missing fonts. Consumers commonly reference
/// <c>SkiaSharp.NativeAssets.Linux.NoDependencies</c>, a libSkiaSharp built without fontconfig, and
/// with that build SkiaSharp can enumerate no system fonts whatsoever. Every text node resolves to
/// nothing and draws nothing.
///
/// That failure is unusually well hidden. <c>fc-list</c> and <c>fc-match</c> work perfectly in the
/// same container, because they are fontconfig's own tools and SkiaSharp never consults them - so a
/// font can be installed, aliased and confirmed present, and still be entirely unavailable to the
/// renderer. It also fails silently: a chart missing every label still looks like a chart, so it
/// passes a glance and reaches whoever asked for it unreadable.
///
/// Two fixes that suggest themselves do not work, and both were measured before this one was
/// written. Installing more fonts changes nothing, because the font manager is empty rather than
/// under-stocked. Naming a font-family changes nothing either: an unresolvable family still draws
/// via a fallback, and comma-separated stacks are not honoured at all - "NoSuchFont, Arial" renders
/// identically to "NoSuchFont", not to Arial.
///
/// Carrying the font removes the dependency on the host entirely, so a chart looks the same
/// wherever it is drawn. That is worth having on its own: the alternative leaves output at the
/// mercy of whatever the platform default happens to be, which can change under a SkiaSharp or
/// base-image update without anything appearing to have changed here.
///
/// Liberation Sans is metrically compatible with Arial, so label widths, wrapping and layout match
/// the reference renderer even though the glyph outlines differ very slightly. It is licensed under
/// the SIL Open Font License 1.1, which permits embedding and redistribution; see Fonts/OFL.txt.
/// </remarks>
internal sealed class EmbeddedTypefaceProvider : ITypefaceProvider
{
	/// <summary>
	/// The family name callers can ask for by name, as well as receiving it as the fallback.
	/// </summary>
	internal const string FamilyName = "Liberation Sans";

	private const string ResourceName = "PanoramicData.ChartMagic.Fonts.LiberationSans-Regular.ttf";

	/// <summary>
	/// The embedded typeface, loaded once.
	/// </summary>
	/// <remarks>
	/// <see cref="SKTypeface"/> is thread-safe for drawing and the font is immutable, so one
	/// instance serves every chart. It is deliberately never disposed: it lives as long as the
	/// process, and disposing it while another render holds it would fault in native code.
	/// </remarks>
	private static readonly Lazy<SKTypeface?> Typeface = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

	/// <summary>
	/// Returns the embedded typeface for every request.
	/// </summary>
	/// <remarks>
	/// Every request, not only unresolvable ones. Registered ahead of the providers that consult
	/// the system, this makes a chart render identically on a developer's Windows machine and in a
	/// Linux container - which matters here, because the output is routinely compared against
	/// another renderer and a font difference would read as a rendering difference.
	///
	/// Returning null would hand the request back to the system providers, which is the behaviour
	/// this exists to avoid.
	/// </remarks>
	public SKTypeface? FromFamilyName(
		string fontFamily,
		SKFontStyleWeight fontWeight,
		SKFontStyleWidth fontWidth,
		SKFontStyleSlant fontStyle)
		=> Typeface.Value;

	private static SKTypeface? Load()
	{
		using var stream = typeof(EmbeddedTypefaceProvider)
			.GetTypeInfo()
			.Assembly
			.GetManifestResourceStream(ResourceName);

		// Null rather than throwing: if the resource is ever lost, falling back to the system
		// providers renders something on a host that has fonts, which beats failing every chart.
		// The accompanying test asserts the resource is present, so this cannot go unnoticed.
		return stream is null ? null : SKTypeface.FromStream(stream);
	}
}
