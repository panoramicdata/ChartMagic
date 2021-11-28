namespace PanoramicData.ChartMagic.Models;

public class AxisArea : ChartNamedElement
{
	public AxisArea(IChartElement parent, string name) : base(parent, name)
	{
	}

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
}
