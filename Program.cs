using System.Net.Http.Headers;
using System.Text.Json;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Hosting;
using AnimalDatabase.Api.OpenApi;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AnimalRepository>();
builder.Services.AddSingleton<ISearchRepository, SearchRepository>();
builder.Services.AddSingleton<IStatsRepository, StatsRepository>();
builder.Services.AddSingleton(new AnAgeSource(Path.Combine(AppContext.BaseDirectory, "Data", "Reference",
    "anage_data.txt")));
builder.Services.AddSingleton(new SeaLifeBaseSource(
    Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "sealifebase_data.txt"),
    Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "fishbase_data.txt")));
builder.Services.AddHttpClient<WikipediaImporter>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "AnimalDatabase/1.0 (https://github.com/Hypnos20011/AnimalDatabase_Server; personal hobby project)");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<EolClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "AnimalDatabase/1.0 (https://github.com/Hypnos20011/AnimalDatabase_Server; personal hobby project)");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<DeeplTranslator>(client =>
{
    client.BaseAddress = new Uri("https://api-free.deepl.com/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("DeepL-Auth-Key", builder.Configuration["DeepL:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddTransient<IBatchTranslator>(sp =>
{
    var primary = sp.GetRequiredService<DeeplTranslator>();
    var baseUrl = sp.GetRequiredService<IConfiguration>()["LibreTranslate:BaseUrl"];
    IBatchTranslator? fallback = string.IsNullOrWhiteSpace(baseUrl) ? null : sp.GetRequiredService<LibreTranslateTranslator>();
    return new FallbackBatchTranslator(primary, fallback);
});
builder.Services.AddTransient<AnimalTextTranslator>();
builder.Services.AddHttpClient<LibreTranslateTranslator>(client =>
{
    var baseUrl = builder.Configuration["LibreTranslate:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConfiguredCors();

builder.Services.AddOptions<ForwardedHeadersOptions>()
    .Configure<IConfiguration>((options, configuration) => ClientIp.ConfigureForwardedHeaders(options, configuration));
builder.Services.AddApiRateLimiting(builder.Configuration);

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

var spaRoot = SpaHosting.ResolveRoot(app.Configuration, app.Environment);

app.UseCloudflareClientIp();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.UseVersionedAssets());
}

if (spaRoot is not null)
{
    app.UseSpaFiles(spaRoot);
}

app.UseCors();
app.UseRateLimiter();
app.MapControllers();
app.MapReservedNotFound();

if (spaRoot is not null)
{
    app.MapSpaFallback(spaRoot);
}

app.Run();