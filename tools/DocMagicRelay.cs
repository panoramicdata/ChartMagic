#!/usr/bin/env dotnet run

// A local relay so the demo can compare its rendering against a DocMagic server.
//
// Run it on the machine the demo is being browsed from, then point the demo at a DocMagic server.
// It is a development tool: it listens on the loopback interface only, and it holds no
// configuration of its own - the target server and the API key arrive with each request, so the
// demo stays the only place any of it is configured.

#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ImplicitUsings=enable

var port = args.Length > 0 && int.TryParse(args[0], out var parsed) ? parsed : 5099;

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls($"http://localhost:{port}");
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

var app = builder.Build();
var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

// Why this exists at all: a browser cannot call the chart endpoint itself. The API key has to go
// in a header, which makes the request preflighted, and the endpoint answers a preflight with 401
// and no cross-origin headers - so the browser refuses before the real request is ever sent. A
// relay forwards it server-side, where none of that applies.
app.Use(async (context, next) =>
{
	var response = context.Response;
	response.Headers["Access-Control-Allow-Origin"] = "*";
	response.Headers["Access-Control-Allow-Headers"] = "content-type,x-api-key,x-target-url";
	response.Headers["Access-Control-Allow-Methods"] = "POST,OPTIONS";

	if (HttpMethods.IsOptions(context.Request.Method))
	{
		response.StatusCode = StatusCodes.Status204NoContent;
		return;
	}

	await next();
});

app.MapPost("/chart", async (HttpRequest request, ILogger<Program> logger) =>
{
	if (!request.Headers.TryGetValue("X-Target-Url", out var targetUrl)
		|| string.IsNullOrWhiteSpace(targetUrl))
	{
		return Results.BadRequest("X-Target-Url header required, naming the DocMagic server.");
	}

	if (!request.Headers.TryGetValue("X-API-KEY", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
	{
		return Results.BadRequest("X-API-KEY header required.");
	}

	// Only ever loopback-to-somewhere, never a general-purpose open proxy: the path is fixed.
	var endpoint = $"{targetUrl.ToString().TrimEnd('/')}/api/chart";

	using var reader = new StreamReader(request.Body);
	var body = await reader.ReadToEndAsync();

	using var forwarded = new HttpRequestMessage(HttpMethod.Post, endpoint)
	{
		Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
	};
	forwarded.Headers.Add("X-API-KEY", apiKey.ToString());
	forwarded.Headers.Add("User-Agent", "ChartMagicDemo/1.0");

	try
	{
		using var upstream = await httpClient.SendAsync(forwarded);
		var bytes = await upstream.Content.ReadAsByteArrayAsync();

		// Log the outcome but never the key or the body: the body is a chart specification, which
		// is large and uninteresting, and the key is a secret.
		logger.LogInformation(
			"{Status} from {Endpoint}, {Length} bytes",
			(int)upstream.StatusCode,
			endpoint,
			bytes.Length);

		if (!upstream.IsSuccessStatusCode)
		{
			return Results.Content(
				System.Text.Encoding.UTF8.GetString(bytes),
				"text/plain",
				statusCode: (int)upstream.StatusCode);
		}

		return Results.File(bytes, "image/png");
	}
	catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
	{
		logger.LogWarning("Could not reach {Endpoint}: {Message}", endpoint, ex.Message);
		return Results.Content($"Could not reach {endpoint}: {ex.Message}", "text/plain", statusCode: 502);
	}
});

app.MapGet("/", () => Results.Content(
	"ChartMagic demo relay. POST a chart specification to /chart with X-Target-Url and X-API-KEY.",
	"text/plain"));

Console.WriteLine($"Relay listening on http://localhost:{port} - loopback only.");
app.Run();
