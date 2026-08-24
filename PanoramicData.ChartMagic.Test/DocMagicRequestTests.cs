using PanoramicData.ChartMagic.Demo.Services;
using System.Drawing;
using System.Text.Json;

namespace PanoramicData.ChartMagic.Test;

/// <summary>
/// The projection from a specification into the request the DocMagic chart endpoint accepts.
/// </summary>
/// <remarks>
/// This decides what the other renderer is asked to draw, so a mistake here does not look like a
/// mistake here: it looks like a rendering difference, which is the one thing the comparison exists
/// to measure. Every assertion below corresponds to something that was wrong on the first attempt
/// and was found by posting to a live server.
///
/// Offline on purpose. The wire format is a fact about the endpoint, established once against a
/// real one; re-establishing it on every test run would make the suite need a Windows server.
/// </remarks>
public class DocMagicRequestTests
{
	private static ChartSpecification Specification() => new()
	{
		InnerPlotXPositionPercent = 10,
		InnerPlotYPositionPercent = 15,
		InnerPlotWidthPercent = 85,
		InnerPlotHeightPercent = 75,
		LegendStyle = LegendStyle.Column,
		SeriesList =
		[
			new SeriesSpecification
			{
				ChartType = SeriesChartType.Column,
				LegendText = "CPU",
				FillColor = Color.SteelBlue,
				StrokeColor = Color.SteelBlue,
				IsXValueIndexed = true,
				Points = [new ChartPoint("Mon", 0, 12), new ChartPoint("Tue", 1, 19)]
			}
		]
	};

	private static JsonElement Build(ChartSpecification specification)
		=> JsonDocument.Parse(DocMagicRequest.Build(specification, 720, 380)).RootElement;

	/// <summary>
	/// Enumerations go as the endpoint's numbers, not as names and not as this library's numbers.
	/// </summary>
	/// <remarks>
	/// Names are rejected outright - there is no string-enumeration converter registered - and the
	/// two libraries number their enumerations differently, so passing this library's value through
	/// would ask for a different chart type and get one without complaint. Column is 9 there and 0
	/// here, which makes it the case worth asserting.
	/// </remarks>
	[Fact]
	public void Enumerations_AreSentAsTheEndpointsNumbers()
	{
		var request = Build(Specification());

		request.GetProperty("SeriesList")[0].GetProperty("ChartType").GetInt32()
			.Should().Be(9, "Column is 9 on the endpoint, where this library calls it 0");

		request.GetProperty("LegendStyle").GetInt32()
			.Should().Be(0, "a column legend is 0 there");
	}

	/// <summary>
	/// Colours go as hex, which is what the endpoint's colour converter reads.
	/// </summary>
	/// <remarks>
	/// Not the "R, G, B" form that the endpoint's own client writes with Newtonsoft: the server
	/// deserialises with System.Text.Json and a converter that reads names and hex only. The two
	/// directions are not symmetrical, which is easy to assume and wrong.
	/// </remarks>
	[Fact]
	public void Colours_AreSentAsHex()
	{
		var request = Build(Specification());

		request.GetProperty("SeriesList")[0].GetProperty("Color").GetString()
			.Should().Be("#4682B4", "SteelBlue as opaque hex");
	}

	/// <summary>
	/// A colour that is not opaque keeps its alpha.
	/// </summary>
	[Fact]
	public void TranslucentColours_KeepTheirAlpha()
	{
		var specification = Specification();
		specification.ChartBackgroundColor = Color.FromArgb(0x37, 0x77, 0x77, 0x77);

		Build(specification).GetProperty("ChartBackgroundColor").GetString()
			.Should().Be("#37777777");
	}

	/// <summary>
	/// Vertical positions are turned upside down.
	/// </summary>
	/// <remarks>
	/// This library measures Y from the bottom of the container and the endpoint from the top, so
	/// copying the number across asks for a different rectangle. A plot 15 up from the bottom and
	/// 75 tall is 10 down from the top.
	/// </remarks>
	[Fact]
	public void VerticalPositions_AreConvertedToMeasureFromTheTop()
	{
		Build(Specification()).GetProperty("InnerPlotYPosition").GetDouble()
			.Should().Be(10, "100 - 15 - 75");
	}

	/// <summary>
	/// The renamed properties arrive under the endpoint's names, and not under this library's.
	/// </summary>
	[Fact]
	public void RenamedProperties_UseTheEndpointsNames()
	{
		var request = Build(Specification());

		request.TryGetProperty("InnerPlotXPosition", out _).Should().BeTrue();
		request.TryGetProperty("InnerPlotXPositionPercent", out _).Should().BeFalse(
			"sending both would leave it ambiguous which one the endpoint honoured");
	}

	/// <summary>
	/// The size, which is a render argument here and a property there.
	/// </summary>
	[Fact]
	public void Size_IsSentAsProperties()
	{
		var request = Build(Specification());

		request.GetProperty("ChartWidth").GetInt32().Should().Be(720);
		request.GetProperty("ChartHeight").GetInt32().Should().Be(380);
		request.GetProperty("ImageFormat").GetInt32().Should().Be(1, "PNG");
	}

	/// <summary>
	/// A labelled point is positioned by its label, an unlabelled one by its number.
	/// </summary>
	/// <remarks>
	/// The label is what makes the axis categorical on the endpoint, so sending the number instead
	/// would produce a numeric axis and a chart that is not comparable.
	/// </remarks>
	[Fact]
	public void LabelledPoints_AreSentByLabel()
	{
		var points = Build(Specification()).GetProperty("SeriesList")[0].GetProperty("Points");

		points[0].GetProperty("XValue").GetString().Should().Be("Mon");
		points[0].GetProperty("YValue").GetDouble().Should().Be(12);
	}

	/// <summary>
	/// An unlabelled point is sent as its number.
	/// </summary>
	[Fact]
	public void UnlabelledPoints_AreSentAsNumbers()
	{
		var specification = Specification();
		specification.SeriesList[0].Points = [new ChartPoint(null, 3, 42)];

		var point = Build(specification).GetProperty("SeriesList")[0].GetProperty("Points")[0];

		point.GetProperty("XValue").GetDouble().Should().Be(3);
	}

	/// <summary>
	/// Every enumeration the specification can carry has a known value on the endpoint.
	/// </summary>
	/// <remarks>
	/// The guard against the tables falling behind. An unmapped member throws rather than
	/// defaulting, so this fails loudly when a member is added - which is the point, because the
	/// alternative is a comparison quietly rendering something else.
	/// </remarks>
	[Fact]
	public void EveryEnumerationValue_HasAKnownEndpointValue()
	{
		var specification = Specification();

		foreach (var property in typeof(ChartSpecification).GetProperties())
		{
			var type = property.PropertyType;
			var underlying = Nullable.GetUnderlyingType(type) ?? type;

			if (!underlying.IsEnum || !property.CanWrite || DocMagicRequest.NotSent.ContainsKey(property.Name))
			{
				continue;
			}

			foreach (var value in Enum.GetValues(underlying))
			{
				property.SetValue(specification, value);

				var act = () => DocMagicRequest.Build(specification, 720, 380);
				act.Should().NotThrow(
					$"{underlying.Name}.{value} has to map to a value the endpoint understands");
			}
		}
	}
}
