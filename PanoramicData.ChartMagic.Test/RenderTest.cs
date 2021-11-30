namespace PanoramicData.ChartMagic.Test;

public class RenderTest
{
	public const int ChartXCount = 20;

	protected static void SaveFile(ChartSpecification chartSpecification, FileInfo fileInfo)
	{
		var chart = chartSpecification.ToChart();
		using var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write);
		chart.SaveImage(fileStream, Enum.Parse<ChartImageFormat>(fileInfo.FullName.Split('.').Last(), true), 1920, 1080, true);
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
		ChartAreaBackgroundColor = Color.Silver,

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
				XPositionPercent = 0,
				YPositionPercent = 100,
				WidthPercent = 20,
				HeightPercent = 5,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontColor = Color.White,
				FillColor = Color.DarkGray,
				StrokeColor = Color.White,
				StrokeWidth = 2,
			},
			new()
			{
				Text = "Middle Center",
				XPositionPercent = 50,
				YPositionPercent = 50,
				WidthPercent = 10,
				HeightPercent = 5,
				VerticalAlignment = VerticalAlignment.Middle,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontColor = Color.White,
				FillColor = Color.DarkGray,
			},
			new()
			{
				Text = "Bottom Right",
				XPositionPercent = 90,
				YPositionPercent = 0,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Right,
				WidthPercent = 20,
				HeightPercent = 5,
				FontColor = Color.White,
				FillColor = Color.DarkGray
			}
		},

		SeriesList = new List<SeriesSpecification>
		{
			RedSeriesSpecification,
			GreenSeriesSpecification,
			BlueSeriesSpecification,
			VioletSeriesSpecification
		}
	};

	internal static SeriesSpecification RedSeriesSpecification = new()
	{
		StrokeColor = Color.Red,
		ChartType = SeriesChartType.Line,
		LabelText = "Woo",
		StrokeWidth = 3,
		IsXValueIndexed = true,
		LegendText = "Yay",
		Points = Enumerable.Range(1, ChartXCount).Select(i => new ChartPoint(null, i, 25 + 3 * Math.Sin((float)i / ChartXCount * 2 * Math.PI))).ToList(),
		MarkerStyle = MarkerStyle.Circle,
		MarkerFillColor = Color.White
	};

	internal static SeriesSpecification GreenSeriesSpecification = new()
	{
		StrokeColor = Color.Green,
		FillColor = Color.DarkGreen,
		ChartType = SeriesChartType.Area,
		LabelText = "Woo2",
		IsXValueIndexed = true,
		LegendText = "Yay2",
		Points = Enumerable.Range(1, ChartXCount).Select(i => new ChartPoint(null, i, 15 + 3 * Math.Sin((float)i / ChartXCount * 2 * Math.PI))).ToList(),
		MarkerStyle = MarkerStyle.Circle
	};

	internal static SeriesSpecification BlueSeriesSpecification = new()
	{
		StrokeColor = Color.Blue,
		StrokeStyle = ChartDashStyle.DashDotDot,
		FillColor = Color.DarkBlue,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3A",
		StrokeWidth = 1,
		IsXValueIndexed = true,
		LegendText = "Yay3A",
		Points = Enumerable.Range(1, ChartXCount).Select(i => new ChartPoint(null, i, 2 + 2 * Math.Sin((float)i / ChartXCount * 2 * Math.PI))).ToList(),
		MarkerStyle = MarkerStyle.Circle
	};

	internal static SeriesSpecification VioletSeriesSpecification = new()
	{
		StrokeColor = Color.Violet,
		FillColor = Color.DarkViolet,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3B",
		StrokeWidth = 1,
		IsXValueIndexed = true,
		LegendText = "Yay3B",
		Points = Enumerable.Range(1, ChartXCount).Select(i => new ChartPoint(null, i, 2 + 2 * Math.Sin((float)i / ChartXCount * 2 * Math.PI))).ToList(),
		MarkerStyle = MarkerStyle.Circle
	};
}
