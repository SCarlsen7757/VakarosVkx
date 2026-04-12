using Vakaros.Vkx.Web.Components;
using Vakaros.Vkx.Web.Client.Services;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<Vakaros.Vkx.Web.Client.Services.UserPreferencesService>();

builder.Services.AddHttpClient<VakarosApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(baseUrl);
});

// BFF proxy: browser WASM calls /api/** → this server → actual API (internal network only)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        routes: [
            new RouteConfig
            {
                RouteId = "api-proxy",
                ClusterId = "api",
                Match = new RouteMatch { Path = "/api/{**catch-all}" }
            }
        ],
        clusters: [
            new ClusterConfig
            {
                ClusterId = "api",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["destination1"] = new DestinationConfig { Address = apiBaseUrl }
                }
            }
        ]
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapReverseProxy();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Vakaros.Vkx.Web.Client._Imports).Assembly);

app.Run();
