using System;

namespace PanoramicData.ChartMagic.Models
{
	/// <summary>Specifies style changes that can automatically be made to a label when the <see cref="P:System.Windows.Forms.DataVisualization.Charting.Axis.LabelAutoFitStyle" /> property is used.</summary>
	[Flags]
	public enum LabelAutoFitStyles
	{
		/// <summary>No label changes are allowed.</summary>
		None = 0x0,

		/// <summary>Label font can be increased.</summary>
		IncreaseFont = 0x1,

		/// <summary>Label font can be decreased.</summary>
		DecreaseFont = 0x2,

		/// <summary>Labels can be staggered.</summary>
		StaggeredLabels = 0x4,

		/// <summary>Labels can be angled in 30 degree steps: 0, 30, 60 and 90.</summary>
		LabelsAngleStep30 = 0x8,

		/// <summary>Labels can be angled in 45 degree steps: 0, 45, and 90.</summary>
		LabelsAngleStep45 = 0x10,

		/// <summary>Labels can be angled in 90 degree steps: 0 and 90.</summary>
		LabelsAngleStep90 = 0x20,

		/// <summary>Labels can be word wrapped.</summary>
		WordWrap = 0x40
	}
}