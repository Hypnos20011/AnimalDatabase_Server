using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Hosting;
using AnimalDatabase.Api.OpenApi;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AnimalRepository>();
builder.Services.AddSingleton<ISearchRepository, SearchRepository>();
builder.Services.AddSingleton(new AnAgeSource(Path.Combine(
    AppContext.BaseDirectory, "Data", "Reference", "anage_data.txt")));
builder.Services.AddHttpClient<WikipediaImporter>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AnimalDatabase/1.0 (https://github.com/Hypnos20011/AnimalDatabase_Server; personal hobby project)");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConfiguredCors();

builder.Services.AddOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>()
    .Configure<IConfiguration>((options, configuration) => ClientIp.ConfigureForwardedHeaders(options, configuration));
builder.Services.AddApiRateLimiting(builder.Configuration);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

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
