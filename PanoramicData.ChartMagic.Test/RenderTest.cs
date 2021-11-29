namespace PanoramicData.ChartMagic.Test;

public class RenderTest
{
	public const int ChartXCount = 20;

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
				XPosition = 0,
				YPosition = 100,
				Width = 20,
				Height = 20,
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
				XPosition = 0,
				YPosition = 0,
				Width = 100,
				Height = 100,
				VerticalAlignment = VerticalAlignment.Middle,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontColor = Color.White,
				FillColor = Color.DarkGray,
			},
			new()
			{
				Text = "Bottom Right",
				XPosition = 0,
				YPosition = 100,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Right,
				Width = 20,
				Height = 20,
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
		XValueType = ChartValueType.Auto,
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
		XValueType = ChartValueType.Auto,
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
		XValueType = ChartValueType.Auto,
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
		XValueType = ChartValueType.Auto,
		Points = Enumerable.Range(1, ChartXCount).Select(i => new ChartPoint(null, i, 2 + 2 * Math.Sin((float)i / ChartXCount * 2 * Math.PI))).ToList(),
		MarkerStyle = MarkerStyle.Circle
	};
}
