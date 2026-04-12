using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Vakaros.Vkx.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMemoryCache();

builder.Services.AddScoped<UserPreferencesService>();

builder.Services.AddHttpClient<VakarosApiClient>(client =>
{
    // Always call the Blazor server (same origin). The server proxies /api/** to the
    // actual API via YARP — the browser never reaches the API directly.
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

await builder.Build().RunAsync();
