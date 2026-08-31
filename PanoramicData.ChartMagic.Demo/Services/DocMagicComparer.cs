using PanoramicData.ChartMagic.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// The result of asking a DocMagic server to draw the same chart.
/// </summary>
/// <param name="ImageDataUri">The rendered PNG, ready to put in an img tag.</param>
/// <param name="Error">Why there is no image, where there is none.</param>
public record ComparisonResult(string? ImageDataUri, string? Error)
{
	/// <summary>
	/// Whether there is an image to show.
	/// </summary>
	public bool HasImage => ImageDataUri is { Length: > 0 };
}

/// <summary>
/// Renders a specification on a DocMagic server, through a relay running on this machine.
/// </summary>
/// <remarks>
/// The relay is not an affectation. A browser cannot call the chart endpoint directly: it needs a
/// custom header for the API key, which makes the request preflighted, and the endpoint answers a
/// preflight with 401 and no cross-origin headers at all - confirmed against a live server before
/// this was written. Nothing the page does can get around that, so the call goes to a small relay
/// on the developer's own machine which forwards it server-side, where cross-origin rules do not
/// apply.
///
/// The relay is deliberately dumb and holds no configuration: the server to call and the key to
/// call it with are sent to it per request. That way the demo remains the single place any of this
/// is configured.
/// </remarks>
public sealed class DocMagicComparer(HttpClient httpClient)
{
	/// <summary>
	/// Renders the specification and returns the image, or the reason there is not one.
	/// </summary>
	public async Task<ComparisonResult> RenderAsync(
		ComparisonConfiguration configuration,
		string apiKey,
		ChartSpecification specification,
		int widthPixels,
		int heightPixels,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		if (!configuration.IsConfigured)
		{
			return new ComparisonResult(null, "No DocMagic server configured.");
		}

		try
		{
			var body = DocMagicRequest.Build(specification, widthPixels, heightPixels);
			using var request = CreateRequest(configuration, apiKey, body);
			using var response = await httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return await FailureResult(response, cancellationToken);
			}

			var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
			if (bytes.Length == 0)
			{
				return new ComparisonResult(null, "The server returned an empty image.");
			}

			return new ComparisonResult($"data:image/png;base64,{Convert.ToBase64String(bytes)}", null);
		}
		catch (HttpRequestException ex)
		{
			// Overwhelmingly the relay not running, which is the normal state of affairs, so it is
			// worth naming rather than reporting as a bare network error.
			return new ComparisonResult(
				null,
				$"Could not reach the relay at {configuration.RelayUrl}. Is it running? ({ex.Message})");
		}
		catch (TaskCanceledException)
		{
			return new ComparisonResult(null, "The render timed out.");
		}
	}

	private static HttpRequestMessage CreateRequest(
		ComparisonConfiguration configuration,
		string apiKey,
		string body)
	{
		var request = new HttpRequestMessage(
			HttpMethod.Post,
			$"{configuration.RelayUrl!.TrimEnd('/')}/chart")
		{
			Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
		};
		request.Headers.Add("X-Target-Url", configuration.ServerUrl);
		request.Headers.Add("X-API-KEY", apiKey);
		return request;
	}

	private static async Task<ComparisonResult> FailureResult(
		HttpResponseMessage response,
		CancellationToken cancellationToken)
	{
		var status = (int)response.StatusCode;
		var detail = Shorten(await response.Content.ReadAsStringAsync(cancellationToken));
		var summary = status switch
		{
			400 => "The server rejected the specification",
			401 or 403 => "The server rejected the API key",
			404 => "No chart endpoint there - is this a Windows DocMagic?",
			>= 500 => "The server failed to render it",
			_ => "The server did not render it"
		};
		var message = detail.Length > 0
			? $"{summary} ({status}): {detail}"
			: $"{summary} ({status})";
		return new ComparisonResult(null, message);
	}

	private static string Shorten(string text)
		=> text.Length <= 300 ? text : text[..300] + "...";
}
