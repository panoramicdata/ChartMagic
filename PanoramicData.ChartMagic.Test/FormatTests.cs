using FluentAssertions;
using PanoramicData.ChartMagic.Models;
using Xunit;

namespace PanoramicData.ChartMagic.Test
{
	public class FormaTests : RenderTest
	{
		[Theory]
		[InlineData(ChartImageFormat.Bmp)]
		//[InlineData(ChartImageFormat.Emf)]
		//[InlineData(ChartImageFormat.Exif)]
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
}
