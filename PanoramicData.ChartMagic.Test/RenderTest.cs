namespace PanoramicData.ChartMagic.Test;

public class RenderTest
{
	protected static void SaveFile(ChartSpecification chartSpecification, FileInfo fileInfo)
	{
		var chart = chartSpecification.ToChart();
		using var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write);
		chart.SaveImage(fileStream, Enum.Parse<ChartImageFormat>(fileInfo.FullName.Split('.').Last(), true), true);
	}

	protected static FileInfo GetTempFileName(ChartImageFormat chartImageFormat)
	{
		var tempFileName = Path.GetTempFileName();
		var tempFile = new FileInfo(tempFileName);
		var newTempFileName = tempFileName + "." + chartImageFormat.ToString().ToLowerInvariant();
		tempFile.MoveTo(newTempFileName);
		return new(newTempFileName);
	}

	protected ChartSpecification BasicChartSpecification = new()
	{
		ChartAreaBackgroundColor = Color.Green,

		LegendBackgroundColor = Color.LightBlue,
		LegendBorderColor = Color.Blue,
		LegendBorderLineDashStyle = ChartDashStyle.DashDotDot,

		InnerPlotBorderColor = Color.Gray,

		XAxisBackgroundColor = Color.Pink,
		YAxisBackgroundColor = Color.Purple,

		SeriesList = new List<SeriesSpecification>
					{
						new()
						{
							Color = Color.Red,
							BorderColor = Color.DarkRed,
							ChartType = SeriesChartType.Line,
							LabelText = "Woo",
							BorderWidth = 2,
							IsXValueIndexed = true,
							LegendText = "Yay",
							XValueType = ChartValueType.Auto,
							Points = new()
							{
								new() { XValue = 1, YValue = 2 },
								new() { XValue = 2, YValue = 4 },
								new() { XValue = 3, YValue = 1 },
								new() { XValue = 4, YValue = 7 },
								new() { XValue = 5, YValue = 3 }
							}
						},
						new()
						{
							Color = Color.Blue,
							BorderColor = Color.DarkBlue,
							ChartType = SeriesChartType.Line,
							LabelText = "Woo2",
							BorderWidth = 2,
							IsXValueIndexed = true,
							LegendText = "Yay2",
							XValueType = ChartValueType.Auto,
							Points = new()
							{
								new() { XValue = 1, YValue = 12 },
								new() { XValue = 2, YValue = 14 },
								new() { XValue = 3, YValue = 11 },
								new() { XValue = 4, YValue = 17 },
								new() { XValue = 5, YValue = 13 }
							}
						}
					}
	};
}
