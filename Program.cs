using AnimalDatabase.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AnimalRepository>();
builder.Services.AddSingleton<SearchRepository>();

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
