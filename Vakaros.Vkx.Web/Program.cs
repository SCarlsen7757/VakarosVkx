using Vakaros.Vkx.Web.Components;
using Vakaros.Vkx.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<UserPreferencesService>();

builder.Services.AddHttpClient<VakarosApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
