using System.Drawing;

namespace PanoramicData.ChartMagic.Models;

public class AxisArea(IChartElement parent, string name) : ChartNamedElement(parent, name)
{
	public AxisAlignment Alignment { get; set; }
	public IntervalAutoMode XAxisIntervalAutoMode { get; set; }
	public DateTimeIntervalType IntervalType { get; set; }
	public double? Interval { get; set; }
	public bool IsAutoFit { get; set; }
	public int LabelAngle { get; set; }
	public LabelAutoFitStyles LabelAutoFitStyle { get; set; }
	public string? Title { get; set; }
	public bool MajorGridEnabled { get; set; }
	public DateTimeIntervalType? MajorGridIntervalType { get; set; }
	public double? MajorGridInterval { get; set; }
	public bool MinorGridEnabled { get; set; }
	public DateTimeIntervalType MinorGridIntervalType { get; set; }
	public bool IsEnabled { get; set; }
	public double? MinorGridInterval { get; set; }
	public string? LabelFormat { get; set; }
	public bool IsLogarithmic { get; set; }

	public double? Min { get; set; }
	public double? Max { get; set; }

	/// <summary>
	/// The colour of the axis line and its tick marks.
	/// </summary>
	/// <remarks>
	/// Issue #31: deliberately separate from <see cref="ChartElement.StrokeColor"/>, which the
	/// renderer applies to the axis strip's background rectangle. Reusing that would have drawn
	/// a box around the axis strip rather than a line along its edge.
	/// </remarks>
	public Color LineColor { get; set; } = Color.FromArgb(0x59, 0x59, 0x59);

	/// <summary>
	/// The width of the axis line and its tick marks, in pixels.
	/// </summary>
	public double LineWidth { get; set; } = 1;

	/// <summary>
	/// The dash pattern of the axis line.
	/// </summary>
	/// <remarks>
	/// Separate from <see cref="ChartElement.StrokeStyle"/> for the same reason as
	/// <see cref="LineColor"/>: that one styles the axis strip background, this one the line.
	/// </remarks>
	public ChartDashStyle LineDashStyle { get; set; }

	/// <summary>
	/// How many minor gridlines fall between one major interval and the next.
	/// </summary>
	public int MinorGridSubdivisions { get; set; } = 5;

	/// <summary>
	/// How far tick marks extend from the axis line, in pixels.
	/// </summary>
	public double TickLengthPixels { get; set; } = 5;

	/// <summary>
	/// The colour of major gridlines, drawn across the plot when <see cref="MajorGridEnabled"/>
	/// is set.
	/// </summary>
	public Color MajorGridColor { get; set; } = Color.FromArgb(0xD9, 0xD9, 0xD9);

	/// <summary>
	/// The colour of minor gridlines, drawn across the plot when <see cref="MinorGridEnabled"/>
	/// is set.
	/// </summary>
	public Color MinorGridColor { get; set; } = Color.FromArgb(0xED, 0xED, 0xED);

	/// <summary>
	/// The width of gridlines, in pixels.
	/// </summary>
	public double GridWidth { get; set; } = 1;

	/// <summary>
	/// Whether the axis line, ticks and tick labels are drawn. The axis strip's background is
	/// drawn regardless.
	/// </summary>
	public bool LabelsEnabled { get; set; } = true;

	/// <summary>
	/// Roughly how many tick marks to aim for when no explicit <see cref="Interval"/> is given.
	/// </summary>
	public int TargetTickCount { get; set; } = 8;

	/// <summary>
	/// Whether large tick labels are abbreviated - 1.5k rather than 1500.
	/// </summary>
	public bool UseShortLabels { get; set; }
}
