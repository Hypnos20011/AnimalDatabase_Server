using System.Globalization;
using System.Threading.RateLimiting;
using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AnimalDatabase.Api.RateLimiting;

public static class RateLimitingSetup
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitSettings>().Bind(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejection;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                PerClient(context, settings => settings.GlobalPerWindow, false));

            options.AddPolicy(RateLimitPolicies.WikipediaImport, context =>
                PerClient(context, settings => settings.WikipediaImportPerWindow, false));
            options.AddPolicy(RateLimitPolicies.Translate, context =>
                PerClient(context, settings => settings.TranslatePerWindow, false));
            options.AddPolicy(RateLimitPolicies.SearchLog, context =>
                PerClient(context, settings => settings.SearchLogPerWindow, true));
            options.AddPolicy(RateLimitPolicies.AdminCheck, context =>
                PerClient(context, settings => settings.AdminCheckPerWindow, false));
        });

        return services;
    }

    private static RateLimitSettings Settings(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
    }

    private static TimeSpan Window(RateLimitSettings settings)
    {
        return TimeSpan.FromSeconds(Math.Max(1, settings.WindowSeconds));
    }

    private static RateLimitPartition<string> PerClient(HttpContext context, Func<RateLimitSettings, int> permits,
        bool allowAdmin)
    {
        var settings = Settings(context);
        if (!settings.Enabled)
        {
            return RateLimitPartition.GetNoLimiter("off");
        }

        if (allowAdmin && HasValidAdminKey(context))
        {
            return RateLimitPartition.GetNoLimiter("admin");
        }

        var limit = Math.Max(1, permits(settings));
        var window = Window(settings);
        return RateLimitPartition.GetFixedWindowLimiter(ClientIp.PartitionKey(context), _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static bool HasValidAdminKey(HttpContext context)
    {
        var configured = context.RequestServices.GetRequiredService<IConfiguration>()[AdminKey.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return false;
        }

        return AdminKey.Matches(configured, context.Request.Headers[AdminKey.HeaderName].ToString());
    }

    private static async ValueTask WriteRejection(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
            ? value
            : Window(Settings(context.HttpContext));
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        response.ContentType = "text/plain; charset=utf-8";
        await response.WriteAsync($"Too many requests. Try again in {seconds} seconds.", cancellationToken);
    }
}