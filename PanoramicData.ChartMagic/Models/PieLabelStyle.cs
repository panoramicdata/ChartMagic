namespace PanoramicData.ChartMagic.Models;

/// <summary>
/// Where the label for a pie or doughnut slice is drawn.
/// </summary>
/// <remarks>
/// The names and the default match the PieLabelStyle custom property of
/// System.Windows.Forms.DataVisualization.Charting, because that is the renderer this output has
/// to match, and the value arrives as one of its strings.
/// </remarks>
public enum PieLabelStyle
{
	/// <summary>Inside the slice. The default, as in the Microsoft chart control.</summary>
	Inside,

	/// <summary>Outside the slice, with a line from the slice to the label.</summary>
	Outside,

	/// <summary>Not drawn.</summary>
	Disabled
}
