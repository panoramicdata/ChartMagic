namespace PanoramicData.ChartMagic.Models;

/// <summary>Specifies the automatic axis interval mode.</summary>
public enum IntervalAutoMode
{
	/// <summary>A fixed number of intervals are always created on the axis.</summary>
	FixedCount,

	/// <summary>The number of axis intervals depends on the axis length.</summary>
	VariableCount
}
