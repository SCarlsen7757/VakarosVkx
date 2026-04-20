using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Scalar.AspNetCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<RaceDetectionService>();
builder.Services.AddScoped<VkxIngestionService>();
builder.Services.AddScoped<StartAnalysisService>();

// AI summary agent — configured via RaceSummary:* settings
var raceSummaryApiKey = builder.Configuration["RaceSummary:ApiKey"];
if (!string.IsNullOrEmpty(raceSummaryApiKey))
{
    var model = builder.Configuration["RaceSummary:Model"] ?? "gpt-4o-mini";
    var endpoint = builder.Configuration["RaceSummary:Endpoint"];

    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var options = endpoint is not null
            ? new OpenAIClientOptions { Endpoint = new Uri(endpoint) }
            : new OpenAIClientOptions();
        var credential = new System.ClientModel.ApiKeyCredential(raceSummaryApiKey);
        var client = new OpenAIClient(credential, options);
        return client.GetChatClient(model).AsIChatClient();
    });
    builder.Services.AddScoped<IRaceSummaryAgent, OpenAiRaceSummaryAgent>();
    builder.Services.AddScoped<RaceSummaryService>();
}

var app = builder.Build();

// Apply pending EF Core migrations and run the idempotent hypertables script at startup.
// Skip when the OpenAPI document generator runs the app (SKIP_DB_MIGRATION is set via MSBuild).
if (Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION") != "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var hypertablesSql = await File.ReadAllTextAsync(
        Path.Combine(AppContext.BaseDirectory, "Data", "Migrations", "hypertables.sql"));
    await db.Database.ExecuteSqlRawAsync(hypertablesSql);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/", context =>
    {
        context.Response.Redirect("/scalar/v1");
        return Task.CompletedTask;
    });

    app.MapGet("/swagger", context =>
    {
        context.Response.Redirect("/scalar/v1");
        return Task.CompletedTask;
    });
}

app.UseAuthorization();

app.UseResponseCompression();

app.MapControllers();

app.Run();
