using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AnimalDatabase.Api.Tests;

public sealed class FixtureHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public List<string> RequestedKeys { get; } = new();

    public List<Uri> RequestedUris { get; } = new();

    public List<string> MissingKeys { get; } = new();

    public Exception? Failure { get; set; }

    private readonly Dictionary<string, Queue<HttpStatusCode>> _failures = new(StringComparer.OrdinalIgnoreCase);

    public void Override(string key, string json) => _overrides[key] = json;

    public void FailNext(string key, HttpStatusCode status)
    {
        if (!_failures.TryGetValue(key, out var queue))
        {
            queue = new Queue<HttpStatusCode>();
            _failures[key] = queue;
        }
        queue.Enqueue(status);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        var key = KeyFor(request.RequestUri!);
        string? json;
        lock (_gate)
        {
            RequestedUris.Add(request.RequestUri!);
            RequestedKeys.Add(key);
            if (_failures.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                return Task.FromResult(new HttpResponseMessage(queue.Dequeue()));
            }
            json = _overrides.TryGetValue(key, out var overridden) ? overridden : ReadFixture(key);
            if (json is null)
            {
                MissingKeys.Add(key);
            }
        }

        if (json is null)
        {
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

    private static string Clean(string value) => Regex.Replace(value.Replace(' ', '_'), @"[:*?""<>|]", "_");

    private static string? ReadFixture(string key)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            key.Replace('/', Path.DirectorySeparatorChar) + ".json");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
