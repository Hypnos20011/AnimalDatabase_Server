using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AnimalDatabase.Api.Tests;

public sealed class FixtureHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public List<string> RequestedKeys { get; } = new();

    public List<string> MissingKeys { get; } = new();

    public Exception? Failure { get; set; }

    public void Override(string key, string json) => _overrides[key] = json;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        var key = KeyFor(request.RequestUri!);
        RequestedKeys.Add(key);

        var json = _overrides.TryGetValue(key, out var overridden) ? overridden : ReadFixture(key);
        if (json is null)
        {
            MissingKeys.Add(key);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    public static string KeyFor(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);

        if (uri.Host.Equals("query.wikidata.org", StringComparison.OrdinalIgnoreCase))
        {
            var qid = Regex.Match(query["query"] ?? string.Empty, @"wd:(Q\d+)").Groups[1].Value;
            return $"sparql/{qid}";
        }

        if (uri.Host.Equals("www.wikidata.org", StringComparison.OrdinalIgnoreCase))
        {
            return $"{query["props"]}/{query["ids"]}";
        }

        const string summaryPath = "/api/rest_v1/page/summary/";
        if (uri.AbsolutePath.StartsWith(summaryPath, StringComparison.Ordinal))
        {
            return "summary/" + Clean(Uri.UnescapeDataString(uri.AbsolutePath[summaryPath.Length..]));
        }

        if (query["generator"] == "search")
        {
            return "search/" + Clean((query["gsrsearch"] ?? string.Empty).ToLowerInvariant());
        }

        return $"{query["prop"]}/{Clean(query["titles"] ?? string.Empty)}";
    }

    private static string Clean(string value) => value.Replace(' ', '_');

    private static string? ReadFixture(string key)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            key.Replace('/', Path.DirectorySeparatorChar) + ".json");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
