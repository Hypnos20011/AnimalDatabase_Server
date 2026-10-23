using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.OpenApi;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AnimalRepository>();
builder.Services.AddSingleton<ISearchRepository, SearchRepository>();
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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Retry-After");
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(ClientIp.ConfigureForwardedHeaders);
builder.Services.AddApiRateLimiting(builder.Configuration);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.UseCloudflareClientIp();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.UseVersionedAssets());
}

app.UseCors();
app.UseRateLimiter();
app.MapControllers();

app.Run();
