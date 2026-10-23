using System.Net;
using AnimalDatabase.Api.Hosting;
using AnimalDatabase.Api.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace AnimalDatabase.Api.Tests;

public class DeploymentConfigTests
{
    private const string Proxy = "172.30.0.10";

    private static async Task<HttpStatusCode> CheckAdmin(HttpClient client, string peer, string? forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
        request.Headers.Add(ApiFactory.PeerHeader, peer);
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static ApiFactory BehindCaddy() => new(settings: new()
    {
        ["ForwardedHeaders:KnownProxies:0"] = Proxy,
        ["RateLimiting:AdminCheckPerWindow"] = "2"
    });

    [Fact]
    public async Task Clients_behind_the_configured_proxy_get_their_own_limit()
    {
        using var factory = BehindCaddy();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await CheckAdmin(client, Proxy, "203.0.113.5"));
        Assert.Equal(HttpStatusCode.Unauthorized, await CheckAdmin(client, Proxy, "203.0.113.5"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await CheckAdmin(client, Proxy, "203.0.113.5"));
        Assert.Equal(HttpStatusCode.Unauthorized, await CheckAdmin(client, Proxy, "203.0.113.6"));
    }

    [Fact]
    public async Task Forwarded_for_from_an_untrusted_peer_is_ignored()
    {
        using var factory = BehindCaddy();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await CheckAdmin(client, "172.30.0.99", "203.0.113.5"));
        Assert.Equal(HttpStatusCode.Unauthorized, await CheckAdmin(client, "172.30.0.99", "203.0.113.6"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await CheckAdmin(client, "172.30.0.99", "203.0.113.7"));
    }

    [Fact]
    public async Task Cloudflare_header_is_ignored_from_a_non_loopback_proxy()
    {
        using var factory = BehindCaddy();
        var client = factory.CreateClient();

        async Task<HttpStatusCode> Send(string cf)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
            request.Headers.Add(ApiFactory.PeerHeader, Proxy);
            request.Headers.Add("CF-Connecting-IP", cf);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await Send("198.51.100.1"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Send("198.51.100.2"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await Send("198.51.100.3"));
    }

    [Fact]
    public void Known_proxies_and_networks_come_from_configuration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = Proxy,
            ["ForwardedHeaders:KnownNetworks"] = "10.1.0.0/16, fd00::/8"
        }).Build();
        var options = new ForwardedHeadersOptions();

        ClientIp.ConfigureForwardedHeaders(options, configuration);

        Assert.Contains(IPAddress.Parse(Proxy), options.KnownProxies);
        Assert.Contains(System.Net.IPNetwork.Parse("10.1.0.0/16"), options.KnownIPNetworks);
        Assert.Contains(System.Net.IPNetwork.Parse("fd00::/8"), options.KnownIPNetworks);
        Assert.Contains(System.Net.IPNetwork.Parse("127.0.0.0/8"), options.KnownIPNetworks);
    }

    [Fact]
    public void Invalid_proxy_addresses_fail_loudly()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "caddy"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => ClientIp.ConfigureForwardedHeaders(new ForwardedHeadersOptions(), configuration));
    }

    [Fact]
    public void Cors_origins_are_read_from_configuration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "http://localhost:4200/",
            ["Cors:AllowedOrigins:1"] = "https://example.org"
        }).Build();

        Assert.Equal(new[] { "http://localhost:4200", "https://example.org" }, CorsSetup.AllowedOrigins(configuration));
    }

    [Fact]
    public async Task Cors_allows_configured_origins_only()
    {
        using var factory = new ApiFactory(settings: new() { ["Cors:AllowedOrigins:0"] = "http://localhost:4200" });
        var client = factory.CreateClient();

        async Task<string?> AllowedOrigin(string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
            request.Headers.Add("Origin", origin);
            using var response = await client.SendAsync(request);
            return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
        }

        Assert.Equal("http://localhost:4200", await AllowedOrigin("http://localhost:4200"));
        Assert.Null(await AllowedOrigin("https://evil.example"));
    }

    [Fact]
    public async Task Development_settings_keep_the_dev_server_origin()
    {
        using var factory = new ApiFactory("Development");
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
        request.Headers.Add("Origin", "http://localhost:4200");

        using var response = await client.SendAsync(request);

        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Production_settings_allow_no_cross_origin_requests_by_default()
    {
        using var factory = new ApiFactory("Production");
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
        request.Headers.Add("Origin", "http://localhost:4200");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
