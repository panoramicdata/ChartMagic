namespace PanoramicData.ChartMagic.Test.Extensions;

internal static class ColorExtensions
{
	/// <summary>
	/// Convert to half the original opacity
	/// </summary>
	/// <param name="color"></param>
	public static Color HalfOpacity(this Color color)
		=> Color.FromArgb(color.A / 2, color.R, color.G, color.B);
}
