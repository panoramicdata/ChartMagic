using System.Drawing;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// The shared fixture for the suites that render to a file: a four-series chart exercising
/// lines, areas, stacked areas, markers, a legend and annotations.
/// </summary>
public class RenderTest
{
	readonly static int ChartXCount = 20;

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

	/// <summary>
	/// Renders a specification to PNG and hands back the bytes, cleaning up after itself.
	/// </summary>
	/// <remarks>
	/// Rendering goes through a file rather than a stream so that the pixel assertions run
	/// against exactly the bytes a consumer would get from <c>SaveImage</c>, which is where both
	/// issue #27 and issue #60 showed up.
	/// </remarks>
	protected static byte[] RenderToPngBytes(ChartSpecification specification)
	{
		var fileInfo = GetTempFileName(ChartImageFormat.Png);
		try
		{
			SaveFile(specification, fileInfo);
			return File.ReadAllBytes(fileInfo.FullName);
		}
		finally
		{
			fileInfo.Refresh();
			if (fileInfo.Exists)
			{
				fileInfo.Delete();
			}
		}
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

		AnnotationList = CornerAnnotations(),

		SeriesList =
		[
			RedSeriesSpecification,
			GreenSeriesSpecification,
			BlueSeriesSpecification,
			VioletSeriesSpecification
		]
	};

	/// <summary>
	/// One annotation in each of three corners, which between them cover every combination of
	/// horizontal and vertical alignment.
	/// </summary>
	private static List<AnnotationSpec> CornerAnnotations() =>
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
	];

	/// <summary>
	/// A sine wave of the given amplitude about the given level, over the shared X range.
	/// </summary>
	private static List<ChartPoint> SineWave(double level, double amplitude)
		=> [.. Enumerable
			.Range(1, ChartXCount)
			.Select(i => new ChartPoint(
				null,
				i,
				level + (amplitude * Math.Sin((float)i / ChartXCount * 2 * Math.PI))))];

	internal readonly static SeriesSpecification RedSeriesSpecification = new()
	{
		StrokeColor = Color.Red,
		ChartType = SeriesChartType.Line,
		LabelText = "Woo",
		StrokeWidth = 3,
		IsXValueIndexed = true,
		LegendText = "Yay",
		Points = SineWave(25, 3),
		MarkerStyle = MarkerStyle.Circle,
		MarkerFillColor = Color.White
	};

	internal readonly static SeriesSpecification GreenSeriesSpecification = new()
	{
		StrokeColor = Color.Green,
		FillColor = Color.DarkGreen,
		ChartType = SeriesChartType.Area,
		LabelText = "Woo2",
		IsXValueIndexed = true,
		LegendText = "Yay2",
		Points = SineWave(15, 3),
		MarkerStyle = MarkerStyle.Circle
	};

	internal readonly static SeriesSpecification BlueSeriesSpecification = new()
	{
		StrokeColor = Color.Blue,
		StrokeStyle = ChartDashStyle.DashDotDot,
		FillColor = Color.DarkBlue,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3A",
		StrokeWidth = 1,
		IsXValueIndexed = true,
		LegendText = "Yay3A",
		Points = SineWave(2, 2),
		MarkerStyle = MarkerStyle.Circle
	};

	internal readonly static SeriesSpecification VioletSeriesSpecification = new()
	{
		StrokeColor = Color.Violet,
		FillColor = Color.DarkViolet,
		ChartType = SeriesChartType.StackedArea,
		LabelText = "Woo3B",
		StrokeWidth = 1,
		IsXValueIndexed = true,
		LegendText = "Yay3B",
		Points = SineWave(2, 2),
		MarkerStyle = MarkerStyle.Circle
	};
}
