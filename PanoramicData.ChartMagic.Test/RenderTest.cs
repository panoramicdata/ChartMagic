namespace PanoramicData.ChartMagic.Test;

public class RenderTest
{
	public const int ChartXCount = 20;

	protected static void SaveFile(ChartSpecification chartSpecification, FileInfo fileInfo)
	{
		var chart = chartSpecification.ToChart();
		using var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write);
		chart.SaveImage(fileStream, Enum.Parse<ChartImageFormat>(fileInfo.FullName.Split('.').Last(), true), 1280, 720, false);
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
		LegendStyle = LegendStyle.Row,

		InnerPlotBorderColor = Color.Gray,

		XAxisBackgroundColor = Color.Pink,
		YAxisBackgroundColor = Color.Purple,

		AnnotationList =
		[
			new()
			{
				Text = "Top Left",
				XPositionPercent = 0,
				YPositionPercent = 100,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FillColor = Color.DarkGray,
				StrokeColor = Color.White,
				StrokeWidth = 2
			},
			new()
			{
				Text = "Middle Center",
				XPositionPercent = 50,
				YPositionPercent = 50,
				VerticalAlignment = VerticalAlignment.Middle,
				HorizontalAlignment = HorizontalAlignment.Center,
				FillColor = Color.DarkGray,
				StrokeColor = Color.Red,
			},
			new()
			{
				Text = "Bottom Right",
				XPositionPercent = 100,
				YPositionPercent = 0,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Right,
				StrokeColor = Color.White,
				FillColor = Color.DarkGray,
				FontFamily = "Arial",
				FontWeight = FontWeight.Bold
			}
		],

		SeriesList =
		[
			RedSeriesSpecification,
			GreenSeriesSpecification,
			BlueSeriesSpecification,
			VioletSeriesSpecification
		]
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
