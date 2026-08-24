using Microsoft.JSInterop;

namespace PanoramicData.ChartMagic.Demo.Services;

/// <summary>
/// Where to find a DocMagic server to compare against, and whether comparing is allowed at all.
/// </summary>
/// <param name="RelayUrl">The local relay that forwards to it.</param>
/// <param name="ServerUrl">The DocMagic server the relay should forward to.</param>
/// <param name="HasKey">Whether an API key is held. The key itself is not exposed.</param>
public record ComparisonConfiguration(string? RelayUrl, string? ServerUrl, bool HasKey)
{
	/// <summary>
	/// Whether there is enough here to attempt a comparison.
	/// </summary>
	public bool IsConfigured => RelayUrl is { Length: > 0 } && ServerUrl is { Length: > 0 } && HasKey;
}

/// <summary>
/// Reads the comparison settings from the query string, remembers them in browser storage, and
/// refuses to hold them anywhere but a local development host.
/// </summary>
/// <remarks>
/// The refusal is the point, and it is enforced rather than documented. This demo is published to
/// a public site, and an API key put into browser storage there would sit in that origin's storage
/// and in the browser's history, on a machine and a browser profile that may not be the developer's
/// own. So the settings are only read, only stored and only used when the page is being served from
/// localhost - anywhere else they are ignored and nothing is written.
///
/// A key in a query string is still a key in a URL, which browsers keep. It is stored so that it
/// need only be typed once, and there is a control to forget it again; the demo never displays it.
/// </remarks>
public sealed class ComparisonSettings(IJSRuntime js)
{
	private const string RelayKey = "chartMagicDemo.relayUrl";
    private const string ServerKey = "chartMagicDemo.docMagicUrl";
	private const string ApiKeyKey = "chartMagicDemo.docMagicApiKey";

	/// <summary>
	/// The relay to use when none is given. It runs on the developer's own machine.
	/// </summary>
	private const string DefaultRelayUrl = "http://localhost:5099";

	/// <summary>
	/// Whether this page is being served from a local development host.
	/// </summary>
	public bool IsLocal { get; private set; }

	/// <summary>
	/// Applies anything in the query string, then reads back what is now stored.
	/// </summary>
	public async Task<ComparisonConfiguration> LoadAsync()
	{
		IsLocal = await js.InvokeAsync<bool>("chartMagicDemo.isLocalHost");
		if (!IsLocal)
		{
			return new ComparisonConfiguration(null, null, false);
		}

		var server = await js.InvokeAsync<string?>("chartMagicDemo.takeQueryParameter", "docmagic");
		var apiKey = await js.InvokeAsync<string?>("chartMagicDemo.takeQueryParameter", "key");
		var relay = await js.InvokeAsync<string?>("chartMagicDemo.takeQueryParameter", "relay");

		if (server is { Length: > 0 })
		{
			await js.InvokeVoidAsync("chartMagicDemo.store", ServerKey, server);
		}

		if (apiKey is { Length: > 0 })
		{
			await js.InvokeVoidAsync("chartMagicDemo.store", ApiKeyKey, apiKey);
		}

		if (relay is { Length: > 0 })
		{
			await js.InvokeVoidAsync("chartMagicDemo.store", RelayKey, relay);
		}

		return await ReadAsync();
	}

	/// <summary>
	/// What is currently stored.
	/// </summary>
	public async Task<ComparisonConfiguration> ReadAsync()
	{
		if (!IsLocal)
		{
			return new ComparisonConfiguration(null, null, false);
		}

		return new ComparisonConfiguration(
			await js.InvokeAsync<string?>("chartMagicDemo.read", RelayKey) is { Length: > 0 } relay
				? relay
				: DefaultRelayUrl,
			await js.InvokeAsync<string?>("chartMagicDemo.read", ServerKey),
			await js.InvokeAsync<string?>("chartMagicDemo.read", ApiKeyKey) is { Length: > 0 });
	}

	/// <summary>
	/// The API key, for the one caller that has to send it.
	/// </summary>
	public async Task<string?> ReadApiKeyAsync()
		=> IsLocal ? await js.InvokeAsync<string?>("chartMagicDemo.read", ApiKeyKey) : null;

	/// <summary>
	/// Forgets everything, so a key need not outlive the session it was needed for.
	/// </summary>
	public async Task ForgetAsync()
	{
		foreach (var key in new[] { RelayKey, ServerKey, ApiKeyKey })
		{
			await js.InvokeVoidAsync("chartMagicDemo.forget", key);
		}
	}
}
