namespace AnimalDatabase.Api.Hosting;

public static class CorsSetup
{
    public const string OriginsKey = "Cors:AllowedOrigins";

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>().Configure<IConfiguration>((options, config) =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(AllowedOrigins(config))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Retry-After");
            });
        });
        return services;
    }

    public static string[] AllowedOrigins(IConfiguration configuration)
    {
        var section = configuration.GetSection(OriginsKey);
        var items = string.IsNullOrWhiteSpace(section.Value)
            ? section.GetChildren().Select(child => child.Value ?? string.Empty)
            : section.Value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return items.Select(item => item.Trim().TrimEnd('/')).Where(item => item.Length > 0).Distinct().ToArray();
    }
}
