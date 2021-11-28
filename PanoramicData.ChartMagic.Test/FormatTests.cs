namespace PanoramicData.ChartMagic.Test;

public class FormatTests : RenderTest
{
	[Theory]
	[InlineData(ChartImageFormat.Bmp)]
	[InlineData(ChartImageFormat.Emf)]
	[InlineData(ChartImageFormat.Gif)]
	[InlineData(ChartImageFormat.Jpeg)]
	[InlineData(ChartImageFormat.Png)]
	[InlineData(ChartImageFormat.Svg)]
	[InlineData(ChartImageFormat.Tiff)]
	public void EachFormat_Succeeds(ChartImageFormat chartImageFormat)
	{
		var fileInfo = GetTempFileName(chartImageFormat);
		try
		{
			SaveFile(BasicChartSpecification, fileInfo);
			fileInfo.Exists.Should().BeTrue();
		}
		finally
		{
			fileInfo.Delete();
		}
	}
}
