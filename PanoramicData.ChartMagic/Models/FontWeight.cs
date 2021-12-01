namespace PanoramicData.ChartMagic.Models;

public enum FontWeight
{
	/// <summary>
	/// The value is inherited from the parent element.
	/// </summary>
	Inherit = 0x0,

	/// <summary>
	/// Same as W400
	/// </summary>
	Normal = 0x1,

	/// <summary>
	/// Same as W700
	/// </summary>
	Bold = 0x2,

	/// <summary>
	/// One font weight darker than the parent element.
	/// </summary>
	Bolder = 0x4,

	/// <summary>
	/// One font weight lighter than the parent element
	/// </summary>
	Lighter = 0x8,

	W100 = 0x100,

	W200 = 0x200,

	W300 = 0x400,

	/// <summary>
	/// Same as Normal
	/// </summary>
	W400 = 0x800,

	W500 = 0x1000,

	W600 = 0x2000,

	/// <summary>
	/// Same as Bold
	/// </summary>
	W700 = 0x4000,

	W800 = 0x8000,

	W900 = 0x10000
}