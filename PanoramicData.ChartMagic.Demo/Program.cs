using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PanoramicData.ChartMagic.Demo;
using PanoramicData.ChartMagic.Demo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The optional comparison against a DocMagic server, which only does anything when the demo is
// being served from localhost and has been given a server to compare against.
builder.Services.AddScoped<ComparisonSettings>();
builder.Services.AddScoped(sp => new DocMagicComparer(new HttpClient { Timeout = TimeSpan.FromMinutes(2) }));

await builder.Build().RunAsync();
