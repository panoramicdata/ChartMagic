namespace PanoramicData.ChartMagic.Test;

public class FormatTests : RenderTest
{
	[Theory]
	[InlineData(ChartImageFormat.Jpeg)]
	[InlineData(ChartImageFormat.Png)]
	[InlineData(ChartImageFormat.Svg)]
	public void EachFormat_Succeeds(ChartImageFormat chartImageFormat)
	{
		var fileInfo = GetTempFileName(chartImageFormat);
		try
		{
			SaveFile(BasicChartSpecification, fileInfo);
			fileInfo.Exists.Should().BeTrue();
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to save file {fileInfo.FullName} with format {chartImageFormat}", ex);
		}
		finally
		{
			fileInfo.Delete();
		}
	}
}
