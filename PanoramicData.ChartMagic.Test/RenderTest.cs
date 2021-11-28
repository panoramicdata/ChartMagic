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

		AnnotationList = new()
		{
			new()
			{
				Text = "Top Left",
				XPosition = 0,
				YPosition = 0,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 100,
				Height = 20,
				FontColor = Color.White,
				FillColor = Color.DarkGray
			},
			new()
			{
				Text = "Middle Center",
				XPosition = 0,
				YPosition = 0,
				VerticalAlignment = VerticalAlignment.Middle,
				HorizontalAlignment = HorizontalAlignment.Center,
				Width = 100,
				Height = 20,
				FontColor = Color.White,
				FillColor = Color.DarkGray
			},
			new()
			{
				Text = "Bottom Right",
				XPosition = 0,
				YPosition = 0,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Right,
				Width = 100,
				Height = 20,
				FontColor = Color.White,
				FillColor = Color.DarkGray
			}
		},

		SeriesList = new List<SeriesSpecification>
		{
			//RedSeriesSpecification,
			//GreenSeriesSpecification,
			BlueSeriesSpecification,
			VioletSeriesSpecification
		}
	};

	public static SeriesSpecification RedSeriesSpecification = new()
	{
		StrokeColor = Color.Red,
		ChartType = SeriesChartType.Line,
		LabelText = "Woo",
		StrokeWidth = 20,
		IsXValueIndexed = true,
		LegendText = "Yay",
		XValueType = ChartValueType.Auto,
		Points = new()
		{
			new(1, 22),
			new(2, 24),
			new(3, 21),
			new(4, 27),
			new(5, 23)
		}
	};

	public static SeriesSpecification GreenSeriesSpecification = new()
	{
		StrokeColor = Color.Green,
		FillColor = Color.DarkGreen,
		ChartType = SeriesChartType.Area,
		LabelText = "Woo2",
		StrokeWidth = 20,
		IsXValueIndexed = true,
		LegendText = "Yay2",
		XValueType = ChartValueType.Auto,
		Points = new()
		{
			new(1, 12),
			new(2, 14),
			new(3, 11),
			new(4, 17),
			new(5, 13)
		}
	};

	public static SeriesSpecification BlueSeriesSpecification = new()
	{
		StrokeColor = Color.Blue,
		FillColor = Color.DarkBlue,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3A",
		StrokeWidth = 10,
		IsXValueIndexed = true,
		LegendText = "Yay3A",
		XValueType = ChartValueType.Auto,
		Points = new()
		{
			new(1, 2),
			new(2, 4),
			new(3, 1),
			new(4, 7),
			new(5, 3)
		},
	};

	public static SeriesSpecification VioletSeriesSpecification = new()
	{
		StrokeColor = Color.Violet,
		FillColor = Color.DarkViolet,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3B",
		StrokeWidth = 10,
		IsXValueIndexed = true,
		LegendText = "Yay3B",
		XValueType = ChartValueType.Auto,
		Points = new()
		{
			new(1, 1),
			new(2, 1),
			new(3, 1),
			new(4, 1),
			new(5, 1)
		},
	};
}
