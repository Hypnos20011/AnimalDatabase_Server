using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace AnimalDatabase.Api.RateLimiting;

public static class ClientIp
{
    public const string CloudflareHeader = "CF-Connecting-IP";
    public const string ForwardedForHeader = "X-Forwarded-For";

    public static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = null;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::1/128"));
    }

    public static IApplicationBuilder UseCloudflareClientIp(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            ApplyCloudflareHeader(context);
            await next();
        });
    }

    public static void ApplyCloudflareHeader(HttpContext context)
    {
        var peer = context.Connection.RemoteIpAddress;
        if (peer is null || !IPAddress.IsLoopback(Normalize(peer)))
        {
            return;
        }
        if (context.Request.Headers.ContainsKey(ForwardedForHeader))
        {
            return;
        }
        var value = context.Request.Headers[CloudflareHeader].ToString().Trim();
        if (IPAddress.TryParse(value, out var client))
        {
            context.Connection.RemoteIpAddress = client;
        }
    }

    public static string PartitionKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        return address is null ? "unknown" : Normalize(address).ToString();
    }

    private static IPAddress Normalize(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}
