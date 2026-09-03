using PanoramicData.ChartMagic.Models;
using System.Drawing;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// A sample and its current rendering status.
/// </summary>
/// <param name="Title">Short name for the sample.</param>
/// <param name="Notes">What the sample is meant to show.</param>
/// <param name="Status">Whether it currently renders as intended.</param>
/// <param name="Specification">The chart to render.</param>
public record ChartSample(string Title, string Notes, SampleStatus Status, ChartSpecification Specification);

/// <summary>
/// How close a sample is to rendering correctly. The demo is a scoreboard as much as a gallery.
/// </summary>
public enum SampleStatus
{
	/// <summary>Renders as intended.</summary>
	Working,

	/// <summary>Renders, but not everything asked for appears.</summary>
	Partial,

	/// <summary>Draws nothing, or nothing useful.</summary>
	NotImplemented
}

/// <summary>
/// The colours a chart needs in order to sit on a light or a dark page.
/// </summary>
/// <param name="AxisLine">Axis lines and tick marks.</param>
/// <param name="AxisLabel">Tick labels, axis titles and legend text.</param>
/// <param name="MajorGrid">Major gridlines.</param>
/// <param name="MinorGrid">Minor gridlines.</param>
/// <param name="Border">The chart border.</param>
public record ChartTheme(Color AxisLine, Color AxisLabel, Color MajorGrid, Color MinorGrid, Color Border)
{
	/// <summary>For a light page.</summary>
	public static ChartTheme Light { get; } = new(
		AxisLine: Color.FromArgb(0x59, 0x59, 0x59),
		AxisLabel: Color.FromArgb(0x33, 0x33, 0x33),
		MajorGrid: Color.FromArgb(0xCC, 0xCC, 0xCC),
		MinorGrid: Color.FromArgb(0xE8, 0xE8, 0xE8),
		Border: Color.FromArgb(0xB0, 0xB0, 0xB0));

	/// <summary>For a dark page.</summary>
	public static ChartTheme Dark { get; } = new(
		AxisLine: Color.FromArgb(0xB8, 0xBC, 0xC2),
		AxisLabel: Color.FromArgb(0xE6, 0xE8, 0xEB),
		MajorGrid: Color.FromArgb(0x4A, 0x4F, 0x57),
		MinorGrid: Color.FromArgb(0x35, 0x39, 0x40),
		Border: Color.FromArgb(0x5A, 0x60, 0x68));
}
