using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class WikipediaImporter
{
    private const string RankFamily = "Q35409";
    private const string AdultRole = "Q78101716";

    private static readonly Dictionary<string, string> IucnStatuses = new()
    {
        ["Q211005"] = "least concern",
        ["Q719675"] = "near threatened",
        ["Q278113"] = "vulnerable",
        ["Q11394"] = "endangered",
        ["Q219127"] = "critically endangered",
        ["Q239509"] = "extinct in the wild",
        ["Q237350"] = "extinct",
        ["Q3245245"] = "data deficient",
    };

    private static readonly (string Class, string[] Taxa)[] ClassRules =
    [
        ("mammal", ["Mammalia"]),
        ("bird", ["Aves"]),
        ("reptile", ["Reptilia", "Squamata", "Testudines", "Crocodilia", "Rhynchocephalia", "Sauropsida"]),
        ("amphibian", ["Amphibia", "Lissamphibia"]),
        ("fish", ["Actinopterygii", "Actinopteri", "Teleostei", "Chondrichthyes", "Elasmobranchii", "Holocephali", "Myxini", "Hyperoartia", "Petromyzontida", "Agnatha", "Dipnoi", "Actinistia", "Coelacanthiformes"]),
    ];

    private static readonly Dictionary<string, decimal> MassToKg = new()
    {
        ["Q11570"] = 1m,
        ["Q41803"] = 0.001m,
        ["Q191118"] = 1000m,
        ["Q100995"] = 0.45359237m,
    };

    private static readonly Dictionary<string, decimal> LengthToCm = new()
    {
        ["Q174728"] = 1m,
        ["Q11573"] = 100m,
        ["Q174789"] = 0.1m,
        ["Q218593"] = 2.54m,
        ["Q3710"] = 30.48m,
    };

    private static readonly Dictionary<string, decimal> TimeToYears = new()
    {
        ["Q577"] = 1m,
        ["Q1092296"] = 1m,
        ["Q5151"] = 1m / 12m,
        ["Q23387"] = 1m / 52m,
        ["Q573"] = 1m / 365m,
    };

    private static readonly string[] FunFactHints =
    {
        "the only ", "unique", "unlike ", "unusual", "largest", "smallest", "longest", "fastest",
        "able to ", "capable of", "known for", "remarkabl", "regenerat", "can survive", "record",
    };

    private readonly HttpClient _http;
    private readonly ILogger<WikipediaImporter> _logger;

    public WikipediaImporter(HttpClient http, ILogger<WikipediaImporter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public static bool TryParseUrl(string input, out string language, out string title)
    {
        language = "en";
        title = string.Empty;
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var match = Regex.Match(uri.Host, @"^(?:(?<lang>[a-z\-]+)\.)?(?:m\.)?wikipedia\.org$", RegexOptions.IgnoreCase);
        if (!match.Success || !uri.AbsolutePath.StartsWith("/wiki/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (match.Groups["lang"].Success && match.Groups["lang"].Value != "www")
        {
            language = match.Groups["lang"].Value.ToLowerInvariant();
        }

        title = Uri.UnescapeDataString(uri.AbsolutePath["/wiki/".Length..]).Replace('_', ' ');
        return title.Length > 0;
    }

    public async Task<WikipediaImportResult?> ImportByNameAsync(string name)
    {
        var title = await SearchTitleAsync("en", name.Trim());
        return title is null ? null : await ImportAsync("en", title, name.Trim());
    }

    public async Task<WikipediaImportResult?> ImportAsync(string language, string title, string? preferredName = null)
    {
        var encodedTitle = Uri.EscapeDataString(title.Replace(' ', '_'));
        using var summary = await GetJsonAsync($"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}");
        if (summary is null)
        {
            return null;
        }

        var root = summary.RootElement;
        if (GetString(root, "type") == "disambiguation")
        {
            return null;
        }

        var pageTitle = GetString(root, "title") ?? title;
        var extract = GetString(root, "extract");
        var result = new WikipediaImportResult
        {
            CommonName = Truncate(pageTitle, 120),
            SourceUrl = root.TryGetProperty("content_urls", out var urls)
                        && urls.TryGetProperty("desktop", out var desktop)
                ? GetString(desktop, "page")
                : null,
            ImageUrl = await GetImageAsync(language, pageTitle),
        };

        var qid = GetString(root, "wikibase_item");
        if (qid is not null)
        {
            await FillFromWikidataAsync(qid, result);
        }

        if (result.ScientificName is not null
            && result.ScientificName.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)
            && !(preferredName ?? title).Equals(pageTitle, StringComparison.OrdinalIgnoreCase))
        {
            var name = preferredName ?? title;
            result.CommonName = Truncate(char.ToUpper(name[0]) + name[1..], 120);
        }

        var funFact = await FindFunFactAsync(language, pageTitle, extract);
        var profile = string.IsNullOrWhiteSpace(extract) ? null : extract.Trim();
        if (funFact is not null)
        {
            profile = profile is null ? $"Fun fact: {funFact}" : $"{profile}\n\nFun fact: {funFact}";
        }
        result.Profile = profile;

        return result;
    }

    private async Task FillFromWikidataAsync(string qid, WikipediaImportResult result)
    {
        var claims = await GetClaimsAsync(qid);
        if (claims is null)
        {
            return;
        }

        result.ScientificName = Truncate(GetStringClaims(claims.Value, "P225").FirstOrDefault(), 150);

        var status = GetItemClaims(claims.Value, "P141").FirstOrDefault();
        if (status is not null)
        {
            result.IucnStatus = IucnStatuses.TryGetValue(status, out var statusName)
                ? statusName
                : await GetStatusFromLabelAsync(status);
        }

        var weights = GetQuantities(claims.Value, "P2067", MassToKg);
        if (weights.Count > 0)
        {
            result.WeightKgMin = Math.Round(weights.Min(), 2);
            result.WeightKgMax = Math.Round(weights.Max(), 2);
        }

        var lengths = GetQuantities(claims.Value, "P2043", LengthToCm);
        if (lengths.Count > 0)
        {
            result.SizeLengthCmMin = RoundLength(lengths.Min());
            result.SizeLengthCmMax = RoundLength(lengths.Max());
        }

        var lifespans = GetQuantities(claims.Value, "P2250", TimeToYears);
        if (lifespans.Count > 0)
        {
            result.LifespanYearsMin = (int)Math.Round(lifespans.Min());
            result.LifespanYearsMax = (int)Math.Round(lifespans.Max());
        }

        var ancestors = await GetAncestorsAsync(qid);
        var family = ancestors.FirstOrDefault(a => a.Rank == RankFamily).Name;
        if (family is not null)
        {
            result.Family ??= Truncate(family, 120);
        }

        result.TaxonomicClass = ClassifyTaxa(ancestors.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    internal static decimal RoundLength(decimal centimetres)
    {
        var rounded = Math.Round(centimetres, 1, MidpointRounding.AwayFromZero);
        return rounded == 0m && centimetres > 0m ? 0.1m : rounded;
    }

    internal static string? ClassifyTaxa(HashSet<string> taxa)
    {
        foreach (var (cls, names) in ClassRules)
        {
            if (names.Any(taxa.Contains))
            {
                return cls;
            }
        }

        if (taxa.Contains("Sarcopterygii") && !taxa.Contains("Tetrapoda"))
        {
            return "fish";
        }

        if (taxa.Contains("Animalia") && !taxa.Contains("Vertebrata"))
        {
            return "invertebrate";
        }

        return null;
    }

    private async Task<string?> SearchTitleAsync(string language, string name)
    {
        using var doc = await GetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&generator=search&gsrlimit=8&gsrnamespace=0&prop=pageprops&ppprop=disambiguation&format=json&formatversion=2&gsrsearch={Uri.EscapeDataString(name)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages))
        {
            return null;
        }

        var titles = pages.EnumerateArray()
            .Where(p => !(p.TryGetProperty("pageprops", out var props) && props.TryGetProperty("disambiguation", out _)))
            .OrderBy(p => p.TryGetProperty("index", out var index) ? index.GetInt32() : int.MaxValue)
            .Select(p => GetString(p, "title"))
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
        return titles.FirstOrDefault(t => t.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? titles.FirstOrDefault();
    }

    private async Task<List<(string? Rank, string Name)>> GetAncestorsAsync(string qid)
    {
        var query = $$"""
            SELECT ?rank ?name WHERE {
              wd:{{qid}} wdt:P171* ?taxon .
              ?taxon wdt:P225 ?name .
              OPTIONAL { ?taxon wdt:P105 ?rank . }
            }
            """;
        var ranks = new List<(string? Rank, string Name)>();
        using var doc = await GetJsonAsync(
            $"https://query.wikidata.org/sparql?format=json&query={Uri.EscapeDataString(query)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("results", out var results)
            || !results.TryGetProperty("bindings", out var bindings))
        {
            return ranks;
        }

        foreach (var row in bindings.EnumerateArray())
        {
            var rank = row.TryGetProperty("rank", out var r) ? GetString(r, "value")?.Split('/').Last() : null;
            var name = row.TryGetProperty("name", out var n) ? GetString(n, "value") : null;
            if (name is not null)
            {
                ranks.Add((rank, name));
            }
        }
        return ranks;
    }

    private async Task<string?> GetImageAsync(string language, string title)
    {
        using var doc = await GetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&prop=pageimages&piprop=thumbnail&pithumbsize=1280&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages)
            || pages.GetArrayLength() == 0
            || !pages[0].TryGetProperty("thumbnail", out var thumbnail))
        {
            return null;
        }
        var source = GetString(thumbnail, "source");
        return source?.Split('?')[0];
    }

    private async Task<string?> FindFunFactAsync(string language, string title, string? extract)
    {
        using var doc = await GetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages)
            || pages.GetArrayLength() == 0)
        {
            return null;
        }

        var text = GetString(pages[0], "extract");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var stopAt = Regex.Match(text, @"\n==\s*(See also|References|Notes|Further reading|External links|In culture|Cultural)", RegexOptions.IgnoreCase);
        if (stopAt.Success)
        {
            text = text[..stopAt.Index];
        }

        var sentences = Regex.Split(Regex.Replace(text, @"\n=+[^=\n]+=+\n", "\n"), @"(?<=[.!?])\s+(?=[A-Z])")
            .Select(s => Regex.Replace(s, @"\s+", " ").Trim())
            .Where(s => s.Length is >= 50 and <= 280 && s.EndsWith('.'))
            .Where(s => extract is null || !extract.Contains(s, StringComparison.Ordinal));

        return sentences.FirstOrDefault(s => FunFactHints.Any(h => s.Contains(h, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<JsonElement?> GetClaimsAsync(string qid)
    {
        using var doc = await GetJsonAsync(
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&props=claims&format=json&ids={Uri.EscapeDataString(qid)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("entities", out var entities)
            || !entities.TryGetProperty(qid, out var entity)
            || !entity.TryGetProperty("claims", out var claims))
        {
            return null;
        }
        return claims.Clone();
    }

    private async Task<JsonDocument?> GetJsonAsync(string url)
    {
        using var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Wikipedia request failed with {Status}: {Url}", (int)response.StatusCode, url);
            return null;
        }
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static IEnumerable<JsonElement> GetDataValues(JsonElement claims, string property)
    {
        if (!claims.TryGetProperty(property, out var list))
        {
            yield break;
        }
        foreach (var claim in list.EnumerateArray())
        {
            if (GetString(claim, "rank") == "deprecated")
            {
                continue;
            }
            if (claim.TryGetProperty("mainsnak", out var snak)
                && snak.TryGetProperty("datavalue", out var dataValue)
                && dataValue.TryGetProperty("value", out var value))
            {
                yield return value;
            }
        }
    }

    private async Task<string?> GetStatusFromLabelAsync(string qid)
    {
        using var doc = await GetJsonAsync(
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&props=labels&languages=en&format=json&ids={qid}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("entities", out var entities)
            || !entities.TryGetProperty(qid, out var entity)
            || !entity.TryGetProperty("labels", out var labels)
            || !labels.TryGetProperty("en", out var en))
        {
            return null;
        }

        var label = GetString(en, "value")?.Trim().ToLowerInvariant();
        if (label is null)
        {
            return null;
        }
        if (label.EndsWith(" species"))
        {
            label = label[..^" species".Length];
        }
        return IucnStatuses.Values.FirstOrDefault(v => v == label);
    }

    private static IEnumerable<string> GetStringClaims(JsonElement claims, string property) =>
        GetDataValues(claims, property)
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!);

    private static IEnumerable<string> GetItemClaims(JsonElement claims, string property) =>
        GetDataValues(claims, property)
            .Where(v => v.ValueKind == JsonValueKind.Object)
            .Select(v => GetString(v, "id"))
            .Where(id => id is not null)
            .Select(id => id!);

    private static List<decimal> GetQuantities(JsonElement claims, string property, Dictionary<string, decimal> factors)
    {
        var values = new List<decimal>();
        if (!claims.TryGetProperty(property, out var list))
        {
            return values;
        }

        foreach (var claim in list.EnumerateArray())
        {
            if (GetString(claim, "rank") == "deprecated" || !IsAdultOrUnspecified(claim))
            {
                continue;
            }
            if (!claim.TryGetProperty("mainsnak", out var snak)
                || !snak.TryGetProperty("datavalue", out var dataValue)
                || !dataValue.TryGetProperty("value", out var value)
                || value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var unit = GetString(value, "unit")?.Split('/').Last();
            if (unit is null || !factors.TryGetValue(unit, out var factor))
            {
                continue;
            }
            foreach (var key in new[] { "amount", "lowerBound", "upperBound" })
            {
                if (decimal.TryParse(GetString(value, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    && number >= 0)
                {
                    values.Add(number * factor);
                }
            }
        }
        return values;
    }

    private static bool IsAdultOrUnspecified(JsonElement claim)
    {
        if (!claim.TryGetProperty("qualifiers", out var qualifiers)
            || !qualifiers.TryGetProperty("P3831", out var roles))
        {
            return true;
        }

        return roles.EnumerateArray().Any(role =>
            role.TryGetProperty("datavalue", out var dv)
            && dv.TryGetProperty("value", out var v)
            && GetString(v, "id") == AdultRole);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
