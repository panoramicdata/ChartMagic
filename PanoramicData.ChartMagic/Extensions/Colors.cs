namespace PanoramicData.ChartMagic.Extensions;

public static class Colors
{
	/// <summary>
	/// Convert to an HTML RGB color string (6 characters)
	/// </summary>
	/// <param name="color"></param>
	public static string ToHex(this Color color)
		=> $"#{color.R:X2}{color.G:X2}{color.B:X2}";

	public readonly static Color Transparent = Color.FromArgb(0, 0, 0, 0);
}
