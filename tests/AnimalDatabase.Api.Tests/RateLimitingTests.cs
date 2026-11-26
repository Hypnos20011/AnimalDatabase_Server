using System.Net;
using System.Net.Http.Json;
using AnimalDatabase.Api.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace AnimalDatabase.Api.Tests;

public class RateLimitingTests
{
    private static HttpRequestMessage Request(HttpMethod method, string url, string? peer = null, string? forwardedFor = null, string? cloudflare = null, string? adminKey = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (peer is not null)
        {
            request.Headers.Add(ApiFactory.PeerHeader, peer);
        }
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }
        if (cloudflare is not null)
        {
            request.Headers.Add("CF-Connecting-IP", cloudflare);
        }
        if (adminKey is not null)
        {
            request.Headers.Add("X-Admin-Key", adminKey);
        }
        if (method == HttpMethod.Post)
        {
            request.Content = JsonContent.Create(new { queryText = "okapi", animalId = 2 });
        }
        return request;
    }

    private static HttpRequestMessage Import(string? peer = null, string? forwardedFor = null, string? cloudflare = null) =>
        Request(HttpMethod.Get, "/api/wikipedia/import?q=", peer, forwardedFor, cloudflare, ApiFactory.TestKey);

    private static async Task<List<HttpStatusCode>> SendMany(HttpClient client, int count, Func<int, HttpRequestMessage> build)
    {
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < count; i++)
        {
            using var response = await client.SendAsync(build(i));
            statuses.Add(response.StatusCode);
        }
        return statuses;
    }

    [Fact]
    public async Task The_eleventh_import_in_a_minute_gets_429_with_retry_after()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var first = await SendMany(client, 10, _ => Import());
        using var eleventh = await client.SendAsync(Import());

        Assert.All(first, status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
        var retryAfter = Assert.Single(eleventh.Headers.GetValues("Retry-After"));
        Assert.InRange(int.Parse(retryAfter), 1, 60);
        Assert.StartsWith("Too many requests.", await eleventh.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Imports_without_a_key_count_towards_the_limit_too()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 11, _ => Request(HttpMethod.Get, "/api/wikipedia/import?q=okapi"));

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    [Fact]
    public async Task Gets_are_not_blocked_at_low_volume()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 60, _ => Request(HttpMethod.Get, "/api/searches/top"));

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.OK, status));
    }

    [Fact]
    public async Task The_global_limit_applies_to_everything_else()
    {
        using var factory = new ApiFactory(settings: new() { ["RateLimiting:GlobalPerWindow"] = "5" });
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 6, _ => Request(HttpMethod.Get, "/api/searches/top"));

        Assert.All(statuses.Take(5), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
    }

    [Fact]
    public async Task The_search_log_allows_30_a_minute()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 31, _ => Request(HttpMethod.Post, "/api/searches"));

        Assert.All(statuses.Take(30), status => Assert.Equal(HttpStatusCode.NoContent, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
    }

    [Fact]
    public async Task A_valid_admin_key_skips_the_search_log_limit()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 40, _ => Request(HttpMethod.Post, "/api/searches", adminKey: ApiFactory.TestKey));

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.NoContent, status));
    }

    [Fact]
    public async Task A_tester_key_does_not_skip_the_search_log_limit()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 31, _ => Request(HttpMethod.Post, "/api/searches", adminKey: ApiFactory.TesterKey));

        Assert.All(statuses.Take(30), status => Assert.Equal(HttpStatusCode.NoContent, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
    }

    [Fact]
    public async Task A_wrong_admin_key_does_not_skip_the_search_log_limit()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 31, _ => Request(HttpMethod.Post, "/api/searches", adminKey: "wrong"));

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
    }

    [Fact]
    public async Task The_admin_check_is_limited_to_stop_guessing()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var guesses = await SendMany(client, 10, i => Request(HttpMethod.Get, "/api/admin/check", adminKey: $"guess-{i}"));
        using var right = await client.SendAsync(Request(HttpMethod.Get, "/api/admin/check", adminKey: ApiFactory.TestKey));

        Assert.All(guesses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, right.StatusCode);
        Assert.True(right.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task The_client_ip_comes_from_the_forwarded_header()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await SendMany(client, 10, _ => Import(peer: "127.0.0.1", forwardedFor: "203.0.113.5"));
        using var sameClient = await client.SendAsync(Import(peer: "127.0.0.1", forwardedFor: "203.0.113.5"));
        using var otherClient = await client.SendAsync(Import(peer: "127.0.0.1", forwardedFor: "203.0.113.6"));
        using var proxyItself = await client.SendAsync(Import(peer: "127.0.0.1"));

        Assert.Equal(HttpStatusCode.TooManyRequests, sameClient.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherClient.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, proxyItself.StatusCode);
    }

    [Fact]
    public async Task A_proxy_chain_through_loopback_resolves_to_the_first_outside_address()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await SendMany(client, 10, _ => Import(peer: "::1", forwardedFor: "203.0.113.5, 127.0.0.1"));
        using var sameClient = await client.SendAsync(Import(peer: "127.0.0.1", forwardedFor: "203.0.113.5"));

        Assert.Equal(HttpStatusCode.TooManyRequests, sameClient.StatusCode);
    }

    [Fact]
    public async Task The_cloudflare_header_is_used_when_there_is_no_forwarded_header()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await SendMany(client, 10, _ => Import(peer: "127.0.0.1", cloudflare: "198.51.100.20"));
        using var sameClient = await client.SendAsync(Import(peer: "127.0.0.1", cloudflare: "198.51.100.20"));
        using var otherClient = await client.SendAsync(Import(peer: "127.0.0.1", cloudflare: "198.51.100.21"));

        Assert.Equal(HttpStatusCode.TooManyRequests, sameClient.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherClient.StatusCode);
    }

    [Fact]
    public async Task Forged_headers_from_a_non_loopback_peer_are_ignored()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 11, i =>
            Import(peer: "192.0.2.50", forwardedFor: $"203.0.113.{i + 1}", cloudflare: $"198.51.100.{i + 1}"));
        using var otherPeer = await client.SendAsync(Import(peer: "192.0.2.51"));

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
        Assert.Equal(HttpStatusCode.BadRequest, otherPeer.StatusCode);
    }

    [Fact]
    public async Task Limits_can_be_switched_off_in_configuration()
    {
        using var factory = new ApiFactory(settings: new() { ["RateLimiting:Enabled"] = "false" });
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 15, _ => Import());

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.BadRequest, status));
    }

    [Fact]
    public async Task Limits_come_from_configuration()
    {
        using var factory = new ApiFactory(settings: new() { ["RateLimiting:WikipediaImportPerWindow"] = "2" });
        var client = factory.CreateClient();

        var statuses = await SendMany(client, 3, _ => Import());

        Assert.Equal(new[] { HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.TooManyRequests }, statuses);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("")]
    public void An_unparsable_cloudflare_header_keeps_the_peer_address(string value)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers["CF-Connecting-IP"] = value;

        ClientIp.ApplyCloudflareHeader(context);

        Assert.Equal("127.0.0.1", ClientIp.PartitionKey(context));
    }

    [Fact]
    public void Ipv4_mapped_addresses_share_a_partition_with_plain_ipv4()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5").MapToIPv6();

        Assert.Equal("203.0.113.5", ClientIp.PartitionKey(context));
    }
}
