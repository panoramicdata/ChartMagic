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

	/// <summary>
	/// A format that cannot be written says so.
	/// </summary>
	/// <remarks>
	/// The imaging library underneath writes PNG, JPEG and WEBP, and returns null rather than
	/// throwing for anything else - so BMP, GIF and TIFF used to surface as a
	/// NullReferenceException from inside SaveImage, which tells a caller nothing about which
	/// format was refused or what to ask for instead. Consuming code has to be able to tell an
	/// unsupported format from a bug, because the substitution it makes depends on knowing which.
	/// </remarks>
	[Theory]
	[InlineData(ChartImageFormat.Bmp)]
	[InlineData(ChartImageFormat.Gif)]
	[InlineData(ChartImageFormat.Tiff)]
	public void UnwritableFormat_ThrowsSayingSo(ChartImageFormat chartImageFormat)
	{
		var fileInfo = GetTempFileName(chartImageFormat);
		try
		{
			var act = () => SaveFile(BasicChartSpecification, fileInfo);

			act.Should().Throw<NotSupportedException>()
				.WithMessage($"*{chartImageFormat}*");
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
}