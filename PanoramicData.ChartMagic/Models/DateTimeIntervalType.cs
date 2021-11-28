namespace PanoramicData.ChartMagic.Models;

public enum DateTimeIntervalType
{
	/// <summary>Automatically determined</summary>
	Auto,

	/// <summary>Interval type is in numerical.</summary>
	Number,

	/// <summary>Interval type is in years.</summary>
	Years,

	/// <summary>Interval type is in months.</summary>
	Months,

	/// <summary>Interval type is in weeks.</summary>
	Weeks,

	/// <summary>Interval type is in days.</summary>
	Days,

	/// <summary>Interval type is in hours.</summary>
	Hours,

	/// <summary>Interval type is in minutes.</summary>
	Minutes,

	/// <summary>Interval type is in seconds.</summary>
	Seconds,

	/// <summary>Interval type is in milliseconds.</summary>
	Milliseconds,

	/// <summary>The IntervalType or IntervalOffsetType property is not set.</summary>
	NotSet
}
