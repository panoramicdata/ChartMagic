using FluentAssertions;
using PanoramicData.ChartMagic.Models;
using Xunit;

namespace PanoramicData.ChartMagic.Test
{
	public class SvgTests : RenderTest
	{
		[Fact]
		public void EmptyChart_Succeeds()
		{
			var fileInfo = GetTempFileName(ChartImageFormat.Svg);
			try
			{
				SaveFile(new(), fileInfo);

				fileInfo.Exists.Should().BeTrue();
			}
			finally
			{
				fileInfo.Delete();
			}
		}

		[Fact]
		public void BasicChartSpecification_Succeeds()
		{
			var fileInfo = GetTempFileName(ChartImageFormat.Svg);
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
