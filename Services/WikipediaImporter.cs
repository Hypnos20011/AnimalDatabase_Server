using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class WikipediaImporter
{
    private const string RankFamily = "Q35409";
    private const string RankGenus = "Q34740";
    private const string AdultRole = "Q78101716";
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RetryBudget = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan OptionalBudget = TimeSpan.FromSeconds(25);

    private static readonly Dictionary<string, string> IucnStatuses = new()
    {
        ["Q211005"] = "least concern",
        ["Q719675"] = "near threatened",
        ["Q278113"] = "vulnerable",
        ["Q11394"] = "endangered",
        ["Q219127"] = "critically endangered",
        ["Q239509"] = "extinct in the wild",
        ["Q237350"] = "extinct",
        ["Q3245245"] = "data deficient"
    };

    private static readonly Dictionary<string, string> IucnCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LC"] = "least concern",
        ["NT"] = "near threatened",
        ["VU"] = "vulnerable",
        ["EN"] = "endangered",
        ["CR"] = "critically endangered",
        ["EW"] = "extinct in the wild",
        ["EX"] = "extinct",
        ["DD"] = "data deficient"
    };

    private static readonly (string Class, string[] Taxa)[] ClassRules =
    [
        ("mammal", ["Mammalia"]),
        ("bird", ["Aves"]),
        ("reptile", ["Reptilia", "Squamata", "Testudines", "Crocodilia", "Rhynchocephalia", "Sauropsida"]),
        ("amphibian", ["Amphibia", "Lissamphibia"]),
        ("fish",
        [
            "Actinopterygii", "Actinopteri", "Teleostei", "Chondrichthyes", "Elasmobranchii", "Holocephali", "Myxini",
            "Hyperoartia", "Petromyzontida", "Agnatha", "Dipnoi", "Actinistia", "Coelacanthiformes"
        ])
    ];

    private static readonly Dictionary<string, decimal> MassToKg = new()
    {
        ["Q11570"] = 1m,
        ["Q41803"] = 0.001m,
        ["Q191118"] = 1000m,
        ["Q100995"] = 0.45359237m
    };

    private static readonly Dictionary<string, decimal> LengthToCm = new()
    {
        ["Q174728"] = 1m,
        ["Q11573"] = 100m,
        ["Q174789"] = 0.1m,
        ["Q218593"] = 2.54m,
        ["Q3710"] = 30.48m
    };

    private static readonly Dictionary<string, decimal> TimeToYears = new()
    {
        ["Q577"] = 1m,
        ["Q1092296"] = 1m,
        ["Q5151"] = 1m / 12m,
        ["Q23387"] = 1m / 52m,
        ["Q573"] = 1m / 365m
    };

    private static readonly string[] BelowFamilyRanks =
    [
        "species", "subspecies", "genus", "subgenus", "tribus", "subtribus", "subfamilia", "supertribus", "infratribus",
        "sectio", "series"
    ];

    private readonly Stopwatch _clock = new();

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
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)) return false;

        var match = Regex.Match(uri.Host, @"^(?:(?<lang>[a-z\-]+)\.)?(?:m\.)?wikipedia\.org$", RegexOptions.IgnoreCase);
        if (!match.Success || !uri.AbsolutePath.StartsWith("/wiki/", StringComparison.OrdinalIgnoreCase)) return false;

        if (match.Groups["lang"].Success && match.Groups["lang"].Value != "www")
            language = match.Groups["lang"].Value.ToLowerInvariant();

        title = Uri.UnescapeDataString(uri.AbsolutePath["/wiki/".Length..]).Replace('_', ' ');
        return title.Length > 0;
    }

    public async Task<WikipediaImportResult?> ImportByNameAsync(string name)
    {
        _clock.Restart();
        var title = await SearchTitleAsync("en", name.Trim());
        return title is null ? null : await ImportCoreAsync("en", title, name.Trim());
    }

    public Task<WikipediaImportResult?> ImportAsync(string language, string title, string? preferredName = null)
    {
        _clock.Restart();
        return ImportCoreAsync(language, title, preferredName);
    }

    private async Task<WikipediaImportResult?> ImportCoreAsync(string language, string title, string? preferredName)
    {
        var encodedTitle = Uri.EscapeDataString(title.Replace(' ', '_'));
        using var summary =
            await GetJsonAsync($"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}");
        if (summary is null) return null;

        var root = summary.RootElement;
        if (GetString(root, "type") == "disambiguation") return null;

        var pageTitle = GetString(root, "title") ?? title;
        var extract = GetString(root, "extract");
        var result = new WikipediaImportResult
        {
            CommonName = Truncate(pageTitle, 120),
            SourceUrl = root.TryGetProperty("content_urls", out var urls)
                        && urls.TryGetProperty("desktop", out var desktop)
                ? GetString(desktop, "page")
                : null
        };

        var english = language == "en";
        var qid = GetString(root, "wikibase_item");
        var imageTask = GetImageAsync(language, pageTitle);
        var claimsTask = qid is null ? Task.FromResult<JsonElement?>(null) : GetClaimsAsync(qid);
        var ancestorsTask =
            qid is null ? Task.FromResult(new List<(string? Rank, string Name)>()) : GetAncestorsAsync(qid);
        var articleTask = GetArticleTextAsync(language, pageTitle);
        var wikitextTask = english ? GetWikitextAsync(language, pageTitle) : Task.FromResult<string?>(null);
        var imageInfoTask = english
            ? GetImageInfoAsync(language, pageTitle)
            : Task.FromResult(new Dictionary<string, ImageInfo>());
        await Task.WhenAll(imageTask, claimsTask, ancestorsTask, articleTask, wikitextTask, imageInfoTask);

        var claims = claimsTask.Result;
        if (claims is not null) await FillFromWikidataAsync(claims.Value, ancestorsTask.Result, result);

        var wikitext = wikitextTask.Result;
        var article = articleTask.Result;
        var sections = article is null ? new List<WikipediaText.Section>() : WikipediaText.SplitSections(article);

        if (wikitext is not null)
        {
            var infobox = WikipediaText.ReadInfobox(wikitext);
            if (result.IucnStatus is null
                && infobox.Status is not null
                && infobox.StatusSystem is not null
                && Regex.IsMatch(infobox.StatusSystem, @"^IUCN\s*3\.1$", RegexOptions.IgnoreCase)
                && IucnCodes.TryGetValue(infobox.Status, out var statusName))
                result.IucnStatus = statusName;

            if (result.Family is null)
                result.Family =
                    Truncate(infobox.Family ?? await FindFamilyFromTaxonomyAsync(language, infobox.TaxonomyStart), 120);
        }

        if (english)
        {
            result.CommonName = ChooseCommonName(result, pageTitle, preferredName ?? title, wikitext, claims);
            var genus = claims is not null && GetItemClaims(claims.Value, "P105").Contains(RankGenus);
            FillFromText(result, sections, genus);
            if (result.RangeRegion is null && wikitext is not null)
            {
                var caption = WikipediaText.ReadInfobox(wikitext).RangeMapCaption;
                if (caption is not null && !Regex.IsMatch(caption,
                        @"\b(?:range|ranges|distribution|map|iucn|red|green|blue|native|extant|historic\w*|former)\b",
                        RegexOptions.IgnoreCase)) result.RangeRegion = WikipediaText.CleanPhrase(caption);
            }
        }
        else if (result.ScientificName is not null
                 && result.ScientificName.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)
                 && !(preferredName ?? title).Equals(pageTitle, StringComparison.OrdinalIgnoreCase))
        {
            result.CommonName = Capitalize(preferredName ?? title);
        }

        result.ImageUrl = ChooseImage(imageTask.Result, imageInfoTask.Result, wikitext);

        var lead = WikipediaText.ProfileFromLead(extract);
        var funFact = WikipediaText.FindFunFact(sections, extract);
        result.Profile = funFact is null
            ? lead
            : lead is null
                ? $"Fun fact: {funFact}"
                : $"{lead}\n\nFun fact: {funFact}";

        return result;
    }

    private static string? ChooseCommonName(
        WikipediaImportResult result,
        string pageTitle,
        string typed,
        string? wikitext,
        JsonElement? claims)
    {
        var latinTitle = result.ScientificName is not null
                         && result.ScientificName.Equals(pageTitle, StringComparison.OrdinalIgnoreCase);
        if (!latinTitle) return result.CommonName;

        var candidates = new List<string>();
        if (wikitext is not null) candidates.AddRange(WikipediaText.BoldLeadNames(wikitext));
        if (claims is not null) candidates.AddRange(GetEnglishTaxonNames(claims.Value));
        candidates = candidates
            .Where(c => !c.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)
                        && !c.Equals(result.ScientificName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var typedKey = NameKey(typed);
        if (candidates.Any(c => NameKey(c) == typedKey)) return Capitalize(typed);
        if (candidates.Count > 0) return Capitalize(candidates[0]);
        return typed.Equals(pageTitle, StringComparison.OrdinalIgnoreCase) ? result.CommonName : Capitalize(typed);
    }

    private static string NameKey(string name)
    {
        return Regex.Replace(name.ToLowerInvariant().Replace('\u2019', '\''), @"[\s\-]+", " ").Trim();
    }

    private static IEnumerable<string> GetEnglishTaxonNames(JsonElement claims)
    {
        if (!claims.TryGetProperty("P1843", out var list)) return [];

        return list.EnumerateArray()
            .Where(c => GetString(c, "rank") != "deprecated")
            .OrderBy(c => GetString(c, "rank") == "preferred" ? 0 : 1)
            .Select(c => c.TryGetProperty("mainsnak", out var snak)
                         && snak.TryGetProperty("datavalue", out var dv)
                         && dv.TryGetProperty("value", out var v)
                         && GetString(v, "language") is "en" or "en-gb" or "en-us"
                ? GetString(v, "text")
                : null)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .ToList();
    }

    private static void FillFromText(WikipediaImportResult result, List<WikipediaText.Section> sections, bool genus)
    {
        if (result.Habitat is null || result.RangeRegion is null)
        {
            var (habitat, range) = WikipediaText.FindHabitatAndRange(sections);
            result.Habitat ??= habitat;
            result.RangeRegion ??= range;
        }

        var species = result.ScientificName is not null && result.ScientificName.Trim().Contains(' ');
        if (!species && !genus) return;

        if (species && result.SizeLengthCmMin is null && result.SizeLengthCmMax is null)
        {
            var length = WikipediaText.FindMeasurement(sections, WikipediaText.Quantity.Length);
            if (length is not null)
            {
                result.SizeLengthCmMin = length.Value.Min is { } min ? RoundLength(min) : null;
                result.SizeLengthCmMax = length.Value.Max is { } max ? RoundLength(max) : null;
            }
        }

        if (species && result.WeightKgMin is null && result.WeightKgMax is null)
        {
            var weight = WikipediaText.FindMeasurement(sections, WikipediaText.Quantity.Weight);
            if (weight is not null)
            {
                result.WeightKgMin = weight.Value.Min is { } min ? RoundWeight(min) : null;
                result.WeightKgMax = weight.Value.Max is { } max ? RoundWeight(max) : null;
            }
        }

        if (result.LifespanYearsMin is null && result.LifespanYearsMax is null)
        {
            var lifespan = WikipediaText.FindMeasurement(sections, WikipediaText.Quantity.Lifespan);
            if (lifespan is not null)
            {
                result.LifespanYearsMin = lifespan.Value.Min is { } min
                    ? (int)Math.Round(min, MidpointRounding.AwayFromZero)
                    : null;
                result.LifespanYearsMax = lifespan.Value.Max is { } max
                    ? (int)Math.Round(max, MidpointRounding.AwayFromZero)
                    : null;
            }
        }
    }

    private async Task FillFromWikidataAsync(JsonElement claims, List<(string? Rank, string Name)> ancestors,
        WikipediaImportResult result)
    {
        result.ScientificName = Truncate(GetStringClaims(claims, "P225").FirstOrDefault(), 150);

        var status = GetItemClaims(claims, "P141").FirstOrDefault();
        if (status is not null)
            result.IucnStatus = IucnStatuses.TryGetValue(status, out var statusName)
                ? statusName
                : await GetStatusFromLabelAsync(status);

        var weights = GetQuantities(claims, "P2067", MassToKg);
        if (weights.Count > 0)
        {
            result.WeightKgMin = Math.Round(weights.Min(), 2);
            result.WeightKgMax = Math.Round(weights.Max(), 2);
        }

        var lengths = GetQuantities(claims, "P2043", LengthToCm);
        if (lengths.Count > 0)
        {
            result.SizeLengthCmMin = RoundLength(lengths.Min());
            result.SizeLengthCmMax = RoundLength(lengths.Max());
        }

        var lifespans = GetQuantities(claims, "P2250", TimeToYears);
        if (lifespans.Count > 0)
        {
            result.LifespanYearsMin = (int)Math.Round(lifespans.Min());
            result.LifespanYearsMax = (int)Math.Round(lifespans.Max());
        }

        var family = ancestors.FirstOrDefault(a => a.Rank == RankFamily).Name;
        if (family is not null) result.Family ??= Truncate(family, 120);

        result.TaxonomicClass = ClassifyTaxa(ancestors.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    internal static decimal RoundLength(decimal centimetres)
    {
        var rounded = Math.Round(centimetres, 1, MidpointRounding.AwayFromZero);
        return rounded == 0m && centimetres > 0m ? 0.1m : rounded;
    }

    internal static decimal RoundWeight(decimal kilograms)
    {
        var rounded = Math.Round(kilograms, 2, MidpointRounding.AwayFromZero);
        return rounded == 0m && kilograms > 0m ? 0.01m : rounded;
    }

    internal static string? ClassifyTaxa(HashSet<string> taxa)
    {
        foreach (var (cls, names) in ClassRules)
            if (names.Any(taxa.Contains))
                return cls;

        if (taxa.Contains("Sarcopterygii") && !taxa.Contains("Tetrapoda")) return "fish";

        if (taxa.Contains("Animalia") && !taxa.Contains("Vertebrata")) return "invertebrate";

        return null;
    }

    internal static string? ChooseImage(string? pageImage, Dictionary<string, ImageInfo> infos, string? wikitext)
    {
        var leadFile = pageImage is null ? null : FileNameFromUrl(pageImage);
        if (leadFile is null || !infos.TryGetValue(FileKey(leadFile), out var lead) || IsPhoto(lead)) return pageImage;

        if (wikitext is null) return pageImage;

        foreach (var file in WikipediaText.ArticleFiles(wikitext))
            if (infos.TryGetValue(FileKey(file), out var info)
                && info.Mime == "image/jpeg"
                && info.Width >= 500
                && info.Height >= 300
                && IsPhoto(info))
            {
                var url = info.Width > 1280 && info.ThumbUrl is not null ? info.ThumbUrl : info.Url;
                return url?.Split('?')[0] ?? pageImage;
            }

        return pageImage;
    }

    internal static bool IsPhoto(ImageInfo info)
    {
        if (info.Mime is "image/svg+xml" or "image/gif" ||
            info.Title.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return false;

        var text = $"{info.Title} {info.ObjectName} {info.Description} {info.Categories} {info.Artist}";
        if (Regex.IsMatch(text,
                @"\b(drawings?|drawn|illustrations?|illustrated|plates?|paintings?|painted|engravings?|engraved|lithograph\w*|woodcuts?|watercolou?rs?|diagrams?|maps?|svg|skeletons?|sketch\w*|artwork|line art|clip ?art|lydekker|bloch|museum specimens?|taxidermy|stamps?|scans? of)\b",
                RegexOptions.IgnoreCase)) return false;

        return !Regex.IsMatch(info.Date ?? string.Empty, @"(?<!\d)(1[0-8]\d\d|19[0-4]\d)(?!\d)")
               && !Regex.IsMatch(info.Description ?? string.Empty,
                   @"\b(?:in|circa|c\.|ca\.|published|printed|dated)\s+(1[0-8]\d\d|19[0-4]\d)(?!\d)",
                   RegexOptions.IgnoreCase);
    }

    private static string? FileNameFromUrl(string url)
    {
        var match = Regex.Match(url, @"/(?:thumb/)?[0-9a-f]/[0-9a-f]{2}/([^/?]+)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }

    private static string FileKey(string name)
    {
        var key = Regex.Replace(name.Trim(), @"^(?:File|Image):", string.Empty, RegexOptions.IgnoreCase)
            .Replace('_', ' ');
        key = Regex.Replace(key, @"\s+", " ");
        return key.Length > 0 ? char.ToUpperInvariant(key[0]) + key[1..] : key;
    }

    private async Task<string?> SearchTitleAsync(string language, string name)
    {
        using var doc = await GetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&generator=search&gsrlimit=8&gsrnamespace=0&prop=pageprops&ppprop=disambiguation&format=json&formatversion=2&gsrsearch={Uri.EscapeDataString(name)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages))
            return null;

        var titles = pages.EnumerateArray()
            .Where(p => !(p.TryGetProperty("pageprops", out var props) &&
                          props.TryGetProperty("disambiguation", out _)))
            .OrderBy(p => p.TryGetProperty("index", out var index) ? index.GetInt32() : int.MaxValue)
            .Select(p => GetString(p, "title"))
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
        return titles.FirstOrDefault(t => t.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
               titles.FirstOrDefault();
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
            return ranks;

        foreach (var row in bindings.EnumerateArray())
        {
            var rank = row.TryGetProperty("rank", out var r) ? GetString(r, "value")?.Split('/').Last() : null;
            var name = row.TryGetProperty("name", out var n) ? GetString(n, "value") : null;
            if (name is not null) ranks.Add((rank, name));
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
            return null;
        var source = GetString(thumbnail, "source");
        return source?.Split('?')[0];
    }

    private async Task<Dictionary<string, ImageInfo>> GetImageInfoAsync(string language, string title)
    {
        var infos = new Dictionary<string, ImageInfo>(StringComparer.OrdinalIgnoreCase);
        using var doc = await TryGetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&generator=images&gimlimit=50&prop=imageinfo&iiprop=url%7Csize%7Cmime%7Cextmetadata&iiurlwidth=1280&iiextmetadatafilter=ImageDescription%7CObjectName%7CCategories%7CDateTimeOriginal%7CArtist&iiextmetadatalanguage=en&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages))
            return infos;

        foreach (var page in pages.EnumerateArray())
        {
            var fileTitle = GetString(page, "title");
            if (fileTitle is null
                || !page.TryGetProperty("imageinfo", out var list)
                || list.ValueKind != JsonValueKind.Array
                || list.GetArrayLength() == 0)
                continue;

            var ii = list[0];
            var meta = ii.TryGetProperty("extmetadata", out var m) ? m : default;
            infos[FileKey(fileTitle)] = new ImageInfo(
                FileKey(fileTitle),
                GetString(ii, "mime"),
                GetInt(ii, "width"),
                GetInt(ii, "height"),
                GetString(ii, "url"),
                GetString(ii, "thumburl"),
                MetaValue(meta, "ObjectName"),
                MetaValue(meta, "ImageDescription"),
                MetaValue(meta, "Categories"),
                MetaValue(meta, "Artist"),
                MetaValue(meta, "DateTimeOriginal"));
        }

        return infos;
    }

    private static string? MetaValue(JsonElement meta, string name)
    {
        if (meta.ValueKind != JsonValueKind.Object || !meta.TryGetProperty(name, out var entry)) return null;
        var value = GetString(entry, "value");
        return value is null ? null : WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", " "));
    }

    private async Task<string?> GetArticleTextAsync(string language, string title)
    {
        using var doc = await TryGetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages)
            || pages.GetArrayLength() == 0)
            return null;

        var text = GetString(pages[0], "extract");
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private async Task<string?> GetWikitextAsync(string language, string title)
    {
        using var doc = await TryGetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages)
            || pages.GetArrayLength() == 0
            || !pages[0].TryGetProperty("revisions", out var revisions)
            || revisions.GetArrayLength() == 0
            || !revisions[0].TryGetProperty("slots", out var slots)
            || !slots.TryGetProperty("main", out var main))
            return null;
        return GetString(main, "content");
    }

    private async Task<string?> FindFamilyFromTaxonomyAsync(string language, string? start)
    {
        var name = start;
        for (var hop = 0; hop < 5 && !string.IsNullOrWhiteSpace(name); hop++)
        {
            var template = await GetWikitextAsync(language, $"Template:Taxonomy/{name}");
            if (template is null) return null;

            var (rank, link, parent) = WikipediaText.ReadTaxonomyTemplate(template);
            if (rank is null) return null;
            if (rank.Equals("familia", StringComparison.OrdinalIgnoreCase))
                return link is not null && Regex.IsMatch(link, @"^[A-Z][a-z]+idae$") ? link : null;
            if (!BelowFamilyRanks.Contains(rank, StringComparer.OrdinalIgnoreCase)) return null;
            name = parent;
        }

        return null;
    }

    private async Task<JsonElement?> GetClaimsAsync(string qid)
    {
        using var doc = await GetJsonAsync(
            $"https://www.wikidata.org/w/api.php?action=wbgetentities&props=claims&format=json&ids={Uri.EscapeDataString(qid)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("entities", out var entities)
            || !entities.TryGetProperty(qid, out var entity)
            || !entity.TryGetProperty("claims", out var claims))
            return null;
        return claims.Clone();
    }

    private async Task<JsonDocument?> TryGetJsonAsync(string url)
    {
        if (_clock.Elapsed > OptionalBudget)
        {
            _logger.LogWarning("Skipping optional Wikipedia request after {Seconds}s: {Url}",
                (int)_clock.Elapsed.TotalSeconds, url);
            return null;
        }

        try
        {
            return await GetJsonAsync(url);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning("Optional Wikipedia request failed ({Error}): {Url}", e.GetType().Name, url);
            return null;
        }
    }

    private async Task<JsonDocument?> GetJsonAsync(string url)
    {
        for (var attempt = 1;; attempt++)
        {
            var canRetry = attempt == 1 && _clock.Elapsed < RetryBudget;
            try
            {
                using var cts = new CancellationTokenSource(AttemptTimeout);
                using var response = await _http.GetAsync(url, cts.Token);
                if ((int)response.StatusCode >= 500 && canRetry)
                {
                    _logger.LogWarning("Wikipedia request failed with {Status}, retrying: {Url}",
                        (int)response.StatusCode, url);
                    await Task.Delay(RetryDelay);
                    continue;
                }

                if ((int)response.StatusCode >= 500)
                    throw new HttpRequestException($"Wikipedia answered {(int)response.StatusCode}", null,
                        response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Wikipedia request failed with {Status}: {Url}", (int)response.StatusCode, url);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && canRetry)
            {
                _logger.LogWarning("Wikipedia request failed ({Error}), retrying: {Url}", e.GetType().Name, url);
                await Task.Delay(RetryDelay);
            }
        }
    }

    private static IEnumerable<JsonElement> GetDataValues(JsonElement claims, string property)
    {
        if (!claims.TryGetProperty(property, out var list)) yield break;
        foreach (var claim in list.EnumerateArray())
        {
            if (GetString(claim, "rank") == "deprecated") continue;
            if (claim.TryGetProperty("mainsnak", out var snak)
                && snak.TryGetProperty("datavalue", out var dataValue)
                && dataValue.TryGetProperty("value", out var value))
                yield return value;
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
            return null;

        var label = GetString(en, "value")?.Trim().ToLowerInvariant();
        if (label is null) return null;
        if (label.EndsWith(" species")) label = label[..^" species".Length];
        return IucnStatuses.Values.FirstOrDefault(v => v == label);
    }

    private static IEnumerable<string> GetStringClaims(JsonElement claims, string property)
    {
        return GetDataValues(claims, property)
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!);
    }

    private static IEnumerable<string> GetItemClaims(JsonElement claims, string property)
    {
        return GetDataValues(claims, property)
            .Where(v => v.ValueKind == JsonValueKind.Object)
            .Select(v => GetString(v, "id"))
            .Where(id => id is not null)
            .Select(id => id!);
    }

    private static List<decimal> GetQuantities(JsonElement claims, string property, Dictionary<string, decimal> factors)
    {
        var values = new List<decimal>();
        if (!claims.TryGetProperty(property, out var list)) return values;

        foreach (var claim in list.EnumerateArray())
        {
            if (GetString(claim, "rank") == "deprecated" || !IsAdultOrUnspecified(claim)) continue;
            if (!claim.TryGetProperty("mainsnak", out var snak)
                || !snak.TryGetProperty("datavalue", out var dataValue)
                || !dataValue.TryGetProperty("value", out var value)
                || value.ValueKind != JsonValueKind.Object)
                continue;

            var unit = GetString(value, "unit")?.Split('/').Last();
            if (unit is null || !factors.TryGetValue(unit, out var factor)) continue;
            foreach (var key in new[] { "amount", "lowerBound", "upperBound" })
                if (decimal.TryParse(GetString(value, key), NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var number)
                    && number >= 0)
                    values.Add(number * factor);
        }

        return values;
    }

    private static bool IsAdultOrUnspecified(JsonElement claim)
    {
        if (!claim.TryGetProperty("qualifiers", out var qualifiers)
            || !qualifiers.TryGetProperty("P3831", out var roles))
            return true;

        return roles.EnumerateArray().Any(role =>
            role.TryGetProperty("datavalue", out var dv)
            && dv.TryGetProperty("value", out var v)
            && GetString(v, "id") == AdultRole);
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(property, out var value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int GetInt(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(property, out var value)
               && value.ValueKind == JsonValueKind.Number
               && value.TryGetInt32(out var number)
            ? number
            : 0;
    }

    private static string Capitalize(string name)
    {
        var trimmed = name.Trim();
        return Truncate(trimmed.Length == 0 ? trimmed : char.ToUpper(trimmed[0]) + trimmed[1..], 120)!;
    }

    private static string? Truncate(string? value, int max)
    {
        return value is null ? null : value.Length <= max ? value : value[..max];
    }

    internal sealed record ImageInfo(
        string Title,
        string? Mime,
        int Width,
        int Height,
        string? Url,
        string? ThumbUrl,
        string? ObjectName,
        string? Description,
        string? Categories,
        string? Artist,
        string? Date);
}

internal static class WikipediaText
{
    private const RegexOptions Ci = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private const string Number = @"(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d+)?";

    private const string Unit =
        @"(?:mm|millimet(?:re|er)s?|cm|centimet(?:re|er)s?|met(?:re|er)s?|m|inch(?:es)?|in(?=\s*(?:[.,;:)]|$))|ft|feet|foot|kg|kilo(?:gram|gramme)s?|g|grams?|grammes?|lbs?|pounds?|oz|ounces?|tonnes?|years?|yrs?)(?![\p{L}/²³])";

    private const string NumberWord =
        @"(?:one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)";

    private static readonly Regex MeasurementPattern = new(
        $@"(?<![\d.,\p{{L}}])(?<a>{Number})(?:\s*-?\s*(?<ua>{Unit}))?(?:\s*(?:–|—|-|to|and)\s*(?<b>{Number}))?\s*-?\s*(?<u>{Unit})",
        Ci);

    private static readonly Regex MaxOnlyPrefix =
        new(
            @"\b(?:up to|as much as|as long as|less than|shorter than|under|below|no more than|a maximum of|not exceeding)\s+(?:about\s+|around\s+|approximately\s+|roughly\s+)?$",
            Ci);

    private static readonly Regex NegatedMinPrefix =
        new(
            @"\b(?:not|never|rarely|seldom)\s+(?:\w+\s+){0,2}(?:longer|more|older)\s+than\s+(?:about\s+|around\s+|approximately\s+|roughly\s+)?$",
            Ci);

    private static readonly Regex MinOnlyPrefix =
        new(
            @"(?:\bover|\bmore than|\bat least|\bexceed\w*|\bin excess of|\blonger than|\babove|\bupwards of)\s+(?:about\s+|around\s+|approximately\s+|roughly\s+)?$",
            Ci);

    private static readonly Regex BodyLengthPhrase = new(
        @"\b(?:head[\s\-–]+and[\s\-–]+body|head[\s\-–]+body|head to tail|from head to tail|tail to beak|from tail to beak|beak to tail|bill to tail|tip to tip|snout[\s\-–]+vent|nose to tail|snout to tail|including (?:the )?tail|total length|body[\s\-]+length)(?: length)?\b",
        Ci);

    private static readonly Regex Connector = new(@"\b(?:and|with|plus|while|whereas|but)\b", Ci);

    private static readonly Regex LengthWords =
        new(@"\b(?:long|length|lengths|measur\w*|grow\w*|reach\w*|bodylength|size|shorter|longer|carapace|shell)\b",
            Ci);

    private static readonly Regex HeightWords = new(@"\b(?:tall|height|heights|shoulders?|stands?|standing)\b", Ci);

    private static readonly Regex WeightWords = new(@"\b(?:weigh\w*|weights?|mass|heavy)\b", Ci);

    private static readonly Regex LifespanWords =
        new(@"\b(?:live|lives|lived|living|lifespans?|life span|life expectancy|longevity|long-lived)\b", Ci);

    private static readonly Regex ExcludedContext = new(
        @"\b(?:tails?|bills?|beaks?|culmen|wings?|wingspans?|legs?|leg-span|tarsal|tarsus|toes?|feet|foot|claws?|ears?|noses?|snouts?|stars?|tongues?|ossicones?|horns?|tusks?|tooth|teeth|canines?|skulls?|hairs?|scales?|fins?|fingers?|digits?|proboscis|eggs?|nests?|burrows?|tunnels?|prey|fish|snakes?|diameter|across|wide|width|deep|depths?|elevations?|altitudes?|distances?|away|high|grains?|bubbles?|span|necks?|eyes?|meat|food|consumes?|consumed|kills?|carcass\w*|home range|territor\w*|clutch)\b",
        Ci);

    private static readonly Regex OutlierClause =
        new(
            @"\b(?:maximum|max|records?|largest|heaviest|longest|biggest|enormous|exceptional\w*|specimens?|fossils?|extinct|other|next|compared|respectively|study|studies|suggested|estimated at)\b",
            Ci);

    private static readonly Regex BirthWords = new(
        @"\b(?:birth|born|newborns?|hatch\w*|neonat\w*|infants?|juveniles?|immature|young|youngs?|baby|babies|cubs?|calf|calves|pups?|chicks?|kittens?|offspring|tadpoles?|larva\w*|fledg\w*|litters?|gestation\w*|embryo\w*|fetus\w*|foetus\w*|eggs?|wean\w*|metamorph\w*|pregnan\w*|gravid)\b",
        Ci);

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7, ["eight"] = 8,
        ["nine"] = 9, ["ten"] = 10,
        ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15, ["sixteen"] = 16,
        ["seventeen"] = 17, ["eighteen"] = 18,
        ["nineteen"] = 19, ["twenty"] = 20, ["thirty"] = 30, ["forty"] = 40, ["fifty"] = 50, ["sixty"] = 60,
        ["seventy"] = 70, ["eighty"] = 80, ["ninety"] = 90
    };

    private static readonly Regex NumberWordYears = new(
        $@"(?<![\p{{L}}\-])(?<w>{NumberWord})(?=(?:\s*[–—]\s*|\s+(?:to|or|and)\s+)(?:{NumberWord}|\d+)\s+years?\b|\s+years?\b)(?![\p{{L}}\-])",
        Ci);

    private static readonly Regex MaturityWords = new(@"\b(?:mature|maturity|maturation|matures)\b", Ci);

    private static readonly Regex LifespanExclusions =
        new(
            @"\b(?:generations?|ago|million|since|until|every|per|old|population|populations|cycle|season|incubat\w*|gestat\w*|months?|days?|weeks?)\b",
            Ci);

    private static readonly Regex SizeSection =
        new(@"descri|characteri|anatomy|morpholog|appearance|physical|size|identification", Ci);

    private static readonly Regex NonLifespanSection =
        new(
            @"taxonom|etymolog|evolution|phylogen|systematic|nomenclature|naming|fossil|distribution|habitat|range|diet|feeding|threat|conservation|status|relation|human|culture|research|population",
            Ci);

    private static readonly Regex HabitatSection =
        new(
            @"^(?:(?:geographic(?:al)? )?(?:distribution|habitat|range)(?: and (?:distribution|habitat|range))?|ecology and distribution|distribution and ecology)$",
            Ci);

    private static readonly Regex FunFactExcludedSection = new(
        @"taxonom|etymolog|evolution|systematic|phylogen|classification|naming|nomenclature|culture|cultural|relationship with humans|human|captivity|as pets?|aquari|conservation|threats?|status|references|further reading|external links|notes|see also|gallery|bibliography",
        Ci);

    private static readonly Regex StopSection =
        new(
            @"^(?:see also|references|notes|footnotes|further reading|external links|bibliography|sources|citations|gallery)$",
            Ci);

    private static readonly Regex HabitatNoun = new(
        @"\b(?:forests?|rainforests?|woodlands?|swamps?|marsh\w*|wetlands?|lakes?|rivers?|streams?|ponds?|pools?|reefs?|deserts?|grasslands?|savann?ahs?|scrub\w*|shrubland\w*|meadows?|seagrass\w*|kelp|slopes?|mountains?|highlands?|lowlands?|coasts?|coastal|waters?|seas?|oceans?|caves?|mangroves?|tundra|steppes?|plains?|islands?|areas?|habitats?|canopy|burrows?|soils?|seabed|seafloor|estuar\w*|lagoons?|bogs?|fens?|heath\w*|hills?|valleys?|cliffs?|beaches|shores?|floodplains?|thickets?|bush\w*|jungles?|uplands?|bamboo|zones?)\b",
        Ci);

    private static readonly string[] Abbreviations =
    [
        "e.g", "i.e", "c", "ca", "approx", "St", "Mt", "Dr", "Mr", "Mrs", "Prof", "vs", "etc", "al", "no", "fig", "sp",
        "spp", "subsp", "var", "cf", "in", "ft", "Jr", "Sr"
    ];

    private static readonly Regex GeographicTail = new(
        @"^(?<h>.*?)\s+(?:in|off|throughout|across|along|around)\s+(?:the\s+)?(?<t>\p{Lu}[\p{L}'\-]*(?:\s+(?:and|of|\p{Lu}[\p{L}'\-]*))*)(?<ft>,?\s+from\s+(?:the\s+)?\p{Lu}[\p{L}'\-]*(?:\s+\p{Lu}[\p{L}'\-]*)*\s+to\s+(?:the\s+)?\p{Lu}[\p{L}'\-]*(?:\s+\p{Lu}[\p{L}'\-]*)*)?");

    internal static List<Section> SplitSections(string extract)
    {
        var sections = new List<Section>();
        var heading = string.Empty;
        var parent = string.Empty;
        var buffer = new List<string>();
        foreach (var raw in extract.Replace("\r", string.Empty).Split('\n'))
        {
            var match = Regex.Match(raw, @"^\s*(={2,6})\s*(.*?)\s*\1\s*$");
            if (!match.Success)
            {
                buffer.Add(raw);
                continue;
            }

            sections.Add(new Section(heading, string.Join("\n", buffer).Trim()));
            buffer.Clear();
            var title = match.Groups[2].Value.Trim();
            if (match.Groups[1].Value.Length == 2)
            {
                parent = title;
                heading = title;
            }
            else
            {
                heading = $"{parent} > {title}";
            }

            if (StopSection.IsMatch(match.Groups[1].Value.Length == 2 ? title : parent))
                return sections.Where(s => s.Text.Length > 0 || s.Heading.Length == 0).ToList();
        }

        sections.Add(new Section(heading, string.Join("\n", buffer).Trim()));
        return sections.Where(s => s.Text.Length > 0 || s.Heading.Length == 0).ToList();
    }

    internal static List<string> Sentences(string? text)
    {
        var sentences = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return sentences;

        var normalized = Regex.Replace(StripCitations(text), @"\s+", " ").Trim();
        var start = 0;
        foreach (Match boundary in Regex.Matches(normalized, @"(?<=[.!?][""”’)]?)\s+(?=[\p{Lu}""“‘(\d])"))
        {
            var candidate = normalized[start..boundary.Index];
            var lastWord = Regex.Match(candidate, @"(\S+)\.$");
            if (lastWord.Success)
            {
                var word = lastWord.Groups[1].Value.TrimStart('(');
                if (Abbreviations.Contains(word, StringComparer.Ordinal)
                    || Regex.IsMatch(word, @"^\p{Lu}$")
                    || Regex.IsMatch(word, @"^(?:\p{L}\.)+\p{L}$"))
                    continue;
            }

            sentences.Add(candidate.Trim());
            start = boundary.Index + boundary.Length;
        }

        sentences.Add(normalized[start..].Trim());
        return sentences.Where(s => s.Length > 0).ToList();
    }

    internal static string StripCitations(string text)
    {
        var cleaned = Regex.Replace(text, @"<ref[^>/]*/>", string.Empty, Ci);
        cleaned = Regex.Replace(cleaned, @"<ref[^>]*>.*?</ref>", string.Empty, Ci | RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        cleaned = RemoveTemplates(cleaned);
        cleaned = Regex.Replace(cleaned, @"\[\[(?:File|Image|Category):[^\[\]]*(?:\[\[[^\]]*\]\][^\[\]]*)*\]\]",
            string.Empty, Ci);
        cleaned = Regex.Replace(cleaned, @"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", "$1");
        cleaned = Regex.Replace(cleaned, @"\[(?:https?:)?//[^\s\]]+\s*([^\]]*)\]", "$1");
        cleaned = Regex.Replace(cleaned,
            @"\[(?:\d+|[a-z]|note \d+|citation needed|clarification needed|verification needed|failed verification|better source needed|when\?|who\?|according to whom\?|dubious[^\]]*|page needed|n \d+)\]",
            string.Empty, Ci);
        cleaned = Regex.Replace(cleaned, @"'{2,}", string.Empty);
        cleaned = Regex.Replace(cleaned, @"<[^>]+>", string.Empty);
        cleaned = WebUtility.HtmlDecode(cleaned);
        cleaned = Regex.Replace(cleaned, @"[ \t]{2,}", " ");
        cleaned = Regex.Replace(cleaned, @"\s+([,.;:])", "$1");
        return cleaned;
    }

    private static string RemoveTemplates(string text)
    {
        var builder = new StringBuilder(text.Length);
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && text[i] == '{' && text[i + 1] == '{')
            {
                depth++;
                i++;
                continue;
            }

            if (depth > 0 && i + 1 < text.Length && text[i] == '}' && text[i + 1] == '}')
            {
                depth--;
                i++;
                continue;
            }

            if (depth == 0) builder.Append(text[i]);
        }

        return builder.ToString();
    }

    internal static (decimal? Min, decimal? Max)? FindMeasurement(List<Section> sections, Quantity quantity)
    {
        foreach (var section in sections)
        {
            var allowed = quantity == Quantity.Lifespan
                ? !NonLifespanSection.IsMatch(LastHeading(section.Heading))
                : section.Heading.Length == 0 || SizeSection.IsMatch(section.Heading);
            if (!allowed) continue;

            var sentences = Sentences(section.Text);
            for (var i = 0; i < sentences.Count; i++)
            {
                var values = MeasureSentence(sentences[i], quantity);
                if (values.Count == 0) continue;

                var male = Regex.IsMatch(sentences[i], @"\bmales?\b", Ci);
                var female = Regex.IsMatch(sentences[i], @"\bfemales?\b", Ci);
                if (male != female && i + 1 < sentences.Count)
                {
                    var other = female ? @"\bmales?\b" : @"\bfemales?\b";
                    if (Regex.IsMatch(sentences[i + 1], other, Ci))
                        values.AddRange(MeasureSentence(sentences[i + 1], quantity));
                }

                var mins = values.Where(v => v.Min is not null).Select(v => v.Min!.Value).ToList();
                var maxes = values.Where(v => v.Max is not null).Select(v => v.Max!.Value).ToList();
                decimal? min = mins.Count > 0 ? mins.Min() : null;
                decimal? max = maxes.Count > 0 ? maxes.Max() : null;
                if (min is not null && max is not null && min > max) continue;
                return (min, max);
            }
        }

        return null;
    }

    internal static List<(decimal? Min, decimal? Max)> MeasureSentence(string sentence, Quantity quantity)
    {
        var found = new List<(decimal? Min, decimal? Max)>();
        if (BirthWords.IsMatch(sentence)) return found;

        var text = sentence;
        string previous;
        do
        {
            previous = text;
            text = Regex.Replace(text, @"\s*\([^()]*\)", string.Empty);
        } while (text != previous);

        text = BodyLengthPhrase.Replace(text, " bodylength ");
        text = Regex.Replace(text, @"\s+", " ");
        if (quantity == Quantity.Lifespan)
            text = NumberWordYears.Replace(text,
                m => NumberWords[m.Groups["w"].Value].ToString(CultureInfo.InvariantCulture));

        var clauses = new List<string>();
        foreach (var piece in Regex.Split(text, @";|,\s+|\s+[—–]\s+|—"))
            if (clauses.Count > 0 && Regex.IsMatch(piece, @"^\s*(?:which|who|such as|that|including)\b", Ci))
                clauses[^1] = $"{clauses[^1]}, {piece}";
            else
                clauses.Add(piece);

        var firstClause = clauses.Count > 0 ? clauses[0] : text;
        foreach (var clause in clauses)
        {
            if (OutlierClause.IsMatch(clause)) continue;

            var matches = MeasurementPattern.Matches(clause).ToList();
            for (var m = 0; m < matches.Count; m++)
            {
                var match = matches[m];
                var unit = match.Groups["u"].Value.ToLowerInvariant();
                var type = UnitType(unit);
                if (type != quantity) continue;
                if (match.Groups["ua"].Success &&
                    UnitType(match.Groups["ua"].Value.ToLowerInvariant()) != type) continue;
                if (match.Groups["ua"].Success
                    && !match.Groups["ua"].Value.Equals(match.Groups["u"].Value, StringComparison.OrdinalIgnoreCase)
                    && Factor(match.Groups["ua"].Value.ToLowerInvariant()) != Factor(unit))
                    continue;

                var before = clause[(m == 0 ? 0 : matches[m - 1].Index + matches[m - 1].Length)..match.Index];
                var connectors = Connector.Matches(before);
                var preceding = connectors.Count > 0
                    ? before[(connectors[^1].Index + connectors[^1].Length)..]
                    : before;
                var after = clause[
                    (match.Index + match.Length)..(m + 1 < matches.Count ? matches[m + 1].Index : clause.Length)];
                var firstConnector = Connector.Match(after);
                var following = firstConnector.Success ? after[..firstConnector.Index] : after;
                var context = $"{preceding} {following}";

                if (MaturityWords.IsMatch(context) || ExcludedContext.IsMatch(context)) continue;

                if (quantity == Quantity.Length)
                {
                    var whole = context.Contains("bodylength", StringComparison.OrdinalIgnoreCase);
                    if (!LengthWords.IsMatch(context)) continue;
                    if (HeightWords.IsMatch(context) &&
                        !Regex.IsMatch(context, @"\b(?:long|length|bodylength)\b", Ci)) continue;
                    if (!whole && ExcludedContext.IsMatch(text)) continue;
                }
                else if (quantity == Quantity.Weight)
                {
                    if (!WeightWords.IsMatch(context) || ExcludedContext.IsMatch(firstClause)) continue;
                }
                else
                {
                    if (!LifespanWords.IsMatch(context) || LifespanExclusions.IsMatch(context)) continue;
                }

                if (!TryNumber(match.Groups["a"].Value, out var a)) continue;
                decimal? b = null;
                if (match.Groups["b"].Success)
                {
                    if (!TryNumber(match.Groups["b"].Value, out var parsed) || parsed < a) continue;
                    b = parsed;
                }

                var factor = Factor(unit);
                var low = a * factor;
                var high = (b ?? a) * factor;
                if (!InBounds(low, quantity) || !InBounds(high, quantity)) continue;

                if (b is null && (MaxOnlyPrefix.IsMatch(before) || NegatedMinPrefix.IsMatch(before)))
                    found.Add((null, high));
                else if (b is null && MinOnlyPrefix.IsMatch(before))
                    found.Add((low, null));
                else
                    found.Add((low, high));
            }
        }

        return found;
    }

    private static bool TryNumber(string value, out decimal number)
    {
        return decimal.TryParse(value.Replace(",", string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture,
            out number);
    }

    private static bool InBounds(decimal value, Quantity quantity)
    {
        return quantity switch
        {
            Quantity.Length => value is >= 0.05m and <= 4000m,
            Quantity.Weight => value is >= 0.0005m and <= 99999.99m,
            _ => value is >= 1m and <= 250m
        };
    }

    private static Quantity? UnitType(string unit)
    {
        return unit switch
        {
            "mm" or "cm" or "m" or "in" or "ft" or "feet" or "foot" => Quantity.Length,
            _ when unit.StartsWith("millimet") || unit.StartsWith("centimet") || unit.StartsWith("met") ||
                   unit.StartsWith("inch") => Quantity.Length,
            "kg" or "g" or "oz" or "lb" or "lbs" => Quantity.Weight,
            _ when unit.StartsWith("kilo") || unit.StartsWith("gram") || unit.StartsWith("pound") ||
                   unit.StartsWith("ounce") || unit.StartsWith("tonne") => Quantity.Weight,
            _ when unit.StartsWith("year") || unit.StartsWith("yr") => Quantity.Lifespan,
            _ => null
        };
    }

    private static decimal Factor(string unit)
    {
        return unit switch
        {
            "mm" => 0.1m,
            "cm" => 1m,
            "m" => 100m,
            "in" => 2.54m,
            "ft" or "feet" or "foot" => 30.48m,
            "kg" => 1m,
            "g" => 0.001m,
            "oz" => 0.028349523125m,
            "lb" or "lbs" => 0.45359237m,
            _ when unit.StartsWith("millimet") => 0.1m,
            _ when unit.StartsWith("centimet") => 1m,
            _ when unit.StartsWith("met") => 100m,
            _ when unit.StartsWith("inch") => 2.54m,
            _ when unit.StartsWith("kilo") => 1m,
            _ when unit.StartsWith("gram") => 0.001m,
            _ when unit.StartsWith("pound") => 0.45359237m,
            _ when unit.StartsWith("ounce") => 0.028349523125m,
            _ when unit.StartsWith("tonne") => 1000m,
            _ => 1m
        };
    }

    internal static (string? Habitat, string? Range) FindHabitatAndRange(List<Section> sections)
    {
        string? habitat = null;
        string? range = null;
        var sources = sections
            .Where(s => HabitatSection.IsMatch(LastHeading(s.Heading)) || HabitatSection.IsMatch(s.Heading))
            .Select(s => Sentences(s.Text).Take(4))
            .Concat(sections.Where(s => s.Heading.Length == 0).Select(s => Sentences(s.Text).Take(6)))
            .Concat(sections.Where(s => Regex.IsMatch(s.Heading, @"ecology", Ci))
                .Select(s => Sentences(s.Text).Take(4)));

        foreach (var sentence in sources.SelectMany(s => s))
        {
            if (Regex.IsMatch(sentence,
                    @"\b(?:historically|formerly|once|originally|fossil|extinct|introduced|captivity|zoos?|aquari\w*|presumed|thought to|were known|now|believed|assumed|has been caught|recorded)\b",
                    Ci)) continue;

            var text = Regex.Replace(sentence, @"\s*\([^()]*\)", string.Empty);
            if (range is null)
            {
                var match = Regex.Match(text, @"\b(?:native|endemic|restricted|confined)\s+(?:only\s+)?to\s+(?<x>.+)",
                    Ci);
                if (!match.Success)
                    match = Regex.Match(text,
                        @"\b(?:found|distributed|occurs?|ranges?|lives?)\s+(?:(?:mainly|mostly|primarily|largely|predominantly|only|exclusively|widely)\s+)?(?:only\s+)?(?<p>in|throughout|across|along|on)\s+(?<x>.+)",
                        Ci);
                if (match.Success)
                {
                    var (head, tail) = SplitGeography(CutPhrase(match.Groups["x"].Value));
                    var lives = Regex.IsMatch(match.Value, @"^lives?\b", Ci);
                    if (tail is not null)
                    {
                        habitat ??= CleanHabitat(head);
                        range = CleanRange(tail);
                    }
                    else if (lives && CleanHabitat(head) is { } livesIn)
                    {
                        habitat ??= livesIn;
                    }
                    else
                    {
                        range = CleanRange(head);
                    }

                    if (range is null && FindGeographicTail(match.Groups["x"].Value) is { } geography)
                    {
                        range = geography.Range;
                        habitat ??= geography.Habitat;
                    }
                }
            }

            if (habitat is null)
            {
                var match = Regex.Match(text,
                    @"\b(?:inhabits?|inhabiting|natural habitats? (?:is|are|consists? of|includes?)|habitats? (?:is|are|consists? of|includes?)|lives? in|dwells? in|prefers?|favou?rs?|(?:is|are) (?:primarily |mainly |mostly |usually |generally )?associated with)\s+(?<x>.+)",
                    Ci);
                if (match.Success)
                {
                    var (head, tail) = SplitGeography(CutPhrase(match.Groups["x"].Value));
                    habitat = CleanHabitat(head);
                    if (habitat is not null && tail is not null && range is null) range = CleanRange(tail);
                }
            }

            if (habitat is not null && range is not null) break;
        }

        return (habitat, range);
    }

    private static (string? Habitat, string Range)? FindGeographicTail(string phrase)
    {
        var clause = Regex.Split(phrase,
            @";|:|\.\s|\.$|,\s*(?:where|which|but|although|though|while|whereas|and|it|they)\b|\s+(?:where|which|that|because|although|though|while|whereas|but)\s",
            Ci)[0];
        var match = GeographicTail.Match(clause);
        if (!match.Success) return null;

        var range = CleanRange(match.Groups["t"].Value + match.Groups["ft"].Value);
        if (range is null || !Regex.IsMatch(match.Groups["t"].Value, @"^\p{Lu}")) return null;

        var place = Regex.Match(match.Groups["h"].Value, @"\b(?:in|on|among|amid)\s+(?<p>[^\p{Lu},;]+)$");
        var habitat = place.Success
            ? CleanHabitat(Regex.Replace(place.Groups["p"].Value, @"\s+and\s+also\s+", " and ", Ci))
            : null;
        return (habitat, range);
    }

    private static (string Head, string? Tail) SplitGeography(string phrase)
    {
        var match = Regex.Match(phrase,
            @"^(?<h>.+?)\s+(?:off|of|in|on|across|throughout|along|around)\s+(?<t>(?:the\s+)?(?:[\p{Ll}\-]+\s+){0,4}?\p{Lu}.*)$");
        if (match.Success
            && HabitatNoun.IsMatch(match.Groups["h"].Value)
            && !Regex.IsMatch(match.Groups["h"].Value, @"\p{Lu}")
            && !Regex.IsMatch(match.Groups["h"].Value,
                @"\b(?:coasts?|coastlines?|islands?|parts?|regions?|portions?|north\w*|south\w*|east\w*|west\w*|central|interior)\b",
                RegexOptions.IgnoreCase))
            return (match.Groups["h"].Value, match.Groups["t"].Value);
        return (phrase, null);
    }

    private static string? CleanRange(string phrase)
    {
        var value = CleanPhrase(phrase);
        return value is not null
               && (Regex.IsMatch(phrase, @"\p{Lu}") || Regex.IsMatch(value,
                   @"\b(?:oceans?|seas?|world|worldwide|tropics|tropical|temperate|arctic|antarctic|hemisphere|continents?)\b",
                   RegexOptions.IgnoreCase))
               && !Regex.IsMatch(value, @"^(?:diverse|various|many|several|some)\b", RegexOptions.IgnoreCase)
            ? value
            : null;
    }

    private static string? CleanHabitat(string phrase)
    {
        var value = CleanPhrase(phrase);
        return value is not null
               && HabitatNoun.IsMatch(value)
               && !Regex.IsMatch(value[1..], @"\p{Lu}")
               && !Regex.IsMatch(value, @"^(?:every|all|most|many|some|various|diverse)\b|\bhabitats$",
                   RegexOptions.IgnoreCase)
            ? value
            : null;
    }

    private static string LastHeading(string heading)
    {
        var index = heading.LastIndexOf(" > ", StringComparison.Ordinal);
        return index < 0 ? heading : heading[(index + 3)..];
    }

    private static string CutPhrase(string phrase)
    {
        var cut = Regex.Split(phrase,
            @",\s*(?:and\s+(?:on|in|from|to|along|at|is|are|can|it|has|was)\b|where|which|from|but|with|predominantly|especially|including|although|though|as well|while|whereas|indicating|except|mostly|mainly|ranging)\b|;|:|\.\s|\.$|\s+(?:where|which|that|ranging|up to|at (?:elevations?|altitudes?|depths?)|above|below|between|during|due|because|although|though|while|whereas|including|except|as well as|but)\b|\s+and\s+(?:can|is|it|has|was|are|occurs?|lives?|eats?|feeds?|is|they|its)\b",
            Ci)[0];
        return cut.Trim().TrimEnd('.', ',', ';', ':').Trim();
    }

    internal static string? CleanPhrase(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return null;

        var value = Regex.Replace(StripCitations(phrase), @"\s+", " ").Trim();
        value = Regex.Replace(value, @"^(?:the|a|an)\s+", string.Empty, Ci);
        value = Regex.Replace(value,
            @"^(?:mainly|mostly|primarily|largely|predominantly|usually|typically|generally)\s+", string.Empty, Ci);
        value = value.Trim().TrimEnd('.', ',', ';', ':');
        if (value.Length < 3
            || value.Length > 100
            || Regex.IsMatch(value, @"[\d\[\]{}|=<>""“”()]")
            || !Regex.IsMatch(value, @"^\p{L}")
            || Regex.IsMatch(value, @"\b(?:it|its|they|their|this|these|species)\b", Ci)
            || Regex.IsMatch(value, @"\b(?:and|or|of|the|in|to|with|from)$", Ci))
            return null;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    internal static string? ProfileFromLead(string? extract)
    {
        if (string.IsNullOrWhiteSpace(extract)) return null;

        var cleaned = RemovePronunciation(extract);
        var sentences = Sentences(cleaned);
        if (sentences.Count == 0) return null;
        var two = sentences.Count > 1 ? $"{sentences[0]} {sentences[1]}" : sentences[0];
        return two.Length <= 350 ? two : sentences[0];
    }

    private static string RemovePronunciation(string text)
    {
        var cleaned = Regex.Replace(text, @"\s*\[[^\[\]]*\]", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s*\((?=[^()]*(?:/|listen|pronunciation|pronounced|IPA))[^()]*\)",
            string.Empty, Ci);
        cleaned = Regex.Replace(cleaned, @"\(\s*[;,]?\s*", "(");
        cleaned = Regex.Replace(cleaned, @"\s*\(\s*\)", string.Empty);
        return cleaned;
    }

    internal static string? FindFunFact(List<Section> sections, string? lead)
    {
        var profile = ProfileFromLead(lead);
        var profileSentences = Sentences(profile);
        var profileWords = Words(profile ?? string.Empty);
        string? best = null;
        var bestScore = int.MinValue;

        foreach (var section in sections)
        {
            if (section.Heading.Length > 0 && FunFactExcludedSection.IsMatch(section.Heading)) continue;

            foreach (var raw in Sentences(RemovePronunciation(section.Text)))
            {
                var sentence = raw.Trim();
                if (!sentence.EndsWith('.') || !Regex.IsMatch(sentence, @"^[\p{Lu}""“]")) continue;
                if (profileSentences.Any(p => p.Equals(sentence, StringComparison.Ordinal))
                    || (profile is not null && profile.Contains(sentence, StringComparison.Ordinal)))
                    continue;

                var words = Words(sentence);
                if (words.Count > 0 && profileWords.Count > 0
                                    && words.Count(w => profileWords.Contains(w)) >= words.Count * 0.5)
                    continue;

                var score = ScoreFunFact(sentence);
                if (score > bestScore)
                {
                    best = sentence;
                    bestScore = score;
                }
            }
        }

        return bestScore >= 3 ? best : null;
    }

    internal static int ScoreFunFact(string sentence)
    {
        var score = 0;
        if (Regex.IsMatch(sentence,
                @"\b(?:largest|smallest|longest|shortest|fastest|slowest|heaviest|lightest|oldest|deepest|highest|biggest|strongest|loudest|rarest|the most|one of the most|the least|the only|only known|unique\w*|unlike|world record|holds? the record|record-breaking|world's)\b",
                Ci)) score += 3;
        if (Regex.IsMatch(sentence,
                @"\b(?:can|able to|capable of|regenerat\w*|survives?|surviving|without|never|despite)\b",
                Ci)) score += 2;
        if (Regex.IsMatch(sentence, @"\d")) score += 1;
        if (Regex.IsMatch(sentence,
                @"\b(?:venom\w*|poison\w*|toxi\w*|glow\w*|biolumin\w*|transparent|translucent|see-through|camouflag\w*|mimic\w*|hibernat\w*|torpor|backwards?|upside[\s-]down|blood|mucus|slime|cannibal\w*|electr\w*|echolocat\w*|tools?|sings?|songs?|clatter\w*|clap\w*|danc\w*|play(?:s|ing)? dead|inflat\w*|spit\w*|squirt\w*|ink|pain|cancer|oxygen|immune|sleep\w*|swallow\w*|hears?|smells?|underwater|pheromone\w*|queen|eusocial|cold-blooded|regrow\w*|limbs?|neoten\w*|gills?|eat\w*|consum\w*|tap\w*|drink\w*|walks?)\b",
                Ci)) score += 2;
        if (Regex.IsMatch(sentence,
                @"\b(?:genus|genera|species was|described by|first described|named after|named for|etymolog\w*|derived from|latin|greek|taxonom\w*|classified|subspecies|phylogen\w*|clade|sister group|synonym\w*|binomial|family\b)",
                Ci)) score -= 4;
        if (Regex.IsMatch(sentence,
                @"\b(?:IUCN|Red List|Least Concern|assessed|endangered|threatened|vulnerable|conservation)\b",
                Ci)) score -= 4;
        if (Regex.IsMatch(sentence, @"\b(?:\d+(?:\.\d+)?\s*(?:mm|cm|m|kg|g|in|ft|lb))\b") && !Regex.IsMatch(sentence,
                @"\b(?:largest|smallest|longest|fastest|heaviest|deepest|the only|world's|unlike)\b", Ci)) score -= 2;
        if (Regex.IsMatch(sentence, @"\b(?:prior to|was previously|formerly|until the discovery)\b", Ci)) score -= 3;
        if (Regex.IsMatch(sentence, @"^(?:Like most|Like other|As with|As in)\b", Ci)) score -= 2;
        if (Regex.IsMatch(sentence, @"\b(?:study|studies|researchers?|reported|report|published|journal)\b",
                Ci)) score -= 1;
        if (Regex.IsMatch(sentence, @"\b(?:1[5-9]\d\d|20\d\d)\b")) score -= 1;
        if (sentence.Length < 60 || sentence.Length > 220) score -= 3;
        if (Regex.IsMatch(sentence,
                @"^(?:However|Further|Furthermore|Also|Additionally|Moreover|In addition|Another|This|These|It is also)\b"))
            score -= 1;
        return score;
    }

    private static HashSet<string> Words(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), @"\p{L}{4,}").Select(m => m.Value).ToHashSet();
    }

    internal static Infobox ReadInfobox(string wikitext)
    {
        var start = Regex.Match(wikitext,
            @"\{\{\s*(?:Speciesbox|Automatic[ _]taxobox|Taxobox|Subspeciesbox|Infraspeciesbox|Hybridbox)\b", Ci);
        if (!start.Success) return new Infobox(null, null, null, null, null);

        var parameters = ReadTemplateParameters(wikitext, start.Index);

        string? Get(string name)
        {
            return parameters.TryGetValue(name, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
        }

        var statusRaw = Get("status");
        var status = statusRaw is null ? null : Regex.Match(StripCitations(statusRaw).Trim(), @"^[A-Z]{2}\b").Value;
        var systemRaw = Get("status_system");
        var system = systemRaw is null ? null : StripCitations(systemRaw).Trim();

        string? family = null;
        var familia = Get("familia");
        if (familia is not null)
        {
            var cleaned = Regex.Replace(StripCitations(familia), @"\s+", " ").Trim();
            family = Regex.IsMatch(cleaned, @"^[A-Z][a-z]+idae$") ? cleaned : null;
        }

        string? taxonomyStart = null;
        var box = start.Value.TrimStart('{').Trim().ToLowerInvariant().Replace('_', ' ');
        if (box is "speciesbox" or "subspeciesbox" or "infraspeciesbox")
            taxonomyStart = Get("genus") ?? Get("parent") ??
                Get("taxon")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        else if (box is "automatic taxobox") taxonomyStart = Get("taxon");
        if (taxonomyStart is not null)
        {
            taxonomyStart = StripCitations(taxonomyStart).Trim();
            if (!Regex.IsMatch(taxonomyStart, @"^[A-Z][a-z]+(?: \([A-Z][a-z]+\))?$")) taxonomyStart = null;
        }

        var caption = Get("range_map_caption");
        return new Infobox(
            string.IsNullOrEmpty(status) ? null : status,
            system,
            family,
            taxonomyStart,
            caption is null ? null : Regex.Replace(StripCitations(caption), @"\s+", " ").Trim());
    }

    internal static (string? Rank, string? Link, string? Parent) ReadTaxonomyTemplate(string wikitext)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(wikitext, @"^\s*\|\s*(?<k>rank|link|parent)\s*=\s*(?<v>[^|\n}]*)",
                     Ci | RegexOptions.Multiline))
            parameters.TryAdd(match.Groups["k"].Value, match.Groups["v"].Value.Trim());

        string? Get(string name)
        {
            return parameters.TryGetValue(name, out var v) && v.Length > 0 ? v : null;
        }

        var link = Get("link");
        if (link is not null) link = Regex.Replace(StripCitations(link), @"\s+", " ").Trim();
        var parent = Get("parent");
        if (parent is not null) parent = parent.Split('/')[0].Trim();
        return (Get("rank")?.Trim(), link, parent);
    }

    private static Dictionary<string, string> ReadTemplateParameters(string wikitext, int start)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var depth = 0;
        var links = 0;
        var current = new StringBuilder();
        var parts = new List<string>();
        for (var i = start; i < wikitext.Length; i++)
        {
            var two = i + 1 < wikitext.Length ? wikitext.Substring(i, 2) : string.Empty;
            if (two == "{{")
            {
                depth++;
                if (depth > 1) current.Append(two);
                i++;
                continue;
            }

            if (two == "}}")
            {
                depth--;
                if (depth == 0)
                {
                    parts.Add(current.ToString());
                    break;
                }

                current.Append(two);
                i++;
                continue;
            }

            if (two == "[[")
            {
                links++;
                current.Append(two);
                i++;
                continue;
            }

            if (two == "]]")
            {
                links = Math.Max(0, links - 1);
                current.Append(two);
                i++;
                continue;
            }

            if (wikitext[i] == '|' && depth == 1 && links == 0)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(wikitext[i]);
        }

        foreach (var part in parts.Skip(1))
        {
            var index = part.IndexOf('=');
            if (index <= 0) continue;
            var key = part[..index].Trim();
            if (key.Length > 0 && !parameters.ContainsKey(key)) parameters[key] = part[(index + 1)..];
        }

        return parameters;
    }

    internal static List<string> BoldLeadNames(string wikitext)
    {
        var body = Regex.Replace(wikitext, @"<ref[^>/]*/>", string.Empty, Ci);
        body = Regex.Replace(body, @"<ref[^>]*>.*?</ref>", string.Empty, Ci | RegexOptions.Singleline);
        body = Regex.Replace(body, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        body = RemoveTemplates(body);
        var headingIndex = Regex.Match(body, @"^==", RegexOptions.Multiline);
        var lead = headingIndex.Success ? body[..headingIndex.Index] : body;

        var names = new List<string>();
        foreach (Match match in Regex.Matches(lead, @"'''''(?<italic>.+?)'''''|'''(?<bold>.+?)'''"))
        {
            if (!match.Groups["bold"].Success) continue;
            var raw = match.Groups["bold"].Value;
            if (raw.StartsWith("''", StringComparison.Ordinal) ||
                raw.Contains("''", StringComparison.Ordinal)) continue;
            var name = Regex.Replace(StripCitations(raw), @"\s+", " ").Trim();
            if (name.Length is >= 2 and <= 80 && Regex.IsMatch(name, @"^[\p{L}][\p{L}\s'’\-.]*$")) names.Add(name);
        }

        return names;
    }

    internal static List<string> ArticleFiles(string wikitext)
    {
        var files = new List<string>();
        var pattern = new Regex(
            @"\|\s*image\d*\s*=\s*(?<a>[^|\n}<]+?\.(?:jpe?g|png|tiff?|webp))\s*(?=[|\n}])|\[\[\s*(?:File|Image)\s*:\s*(?<b>[^|\]]+)|^\s*(?:File|Image)\s*:\s*(?<c>[^|\n]+?\.(?:jpe?g|png|tiff?|webp))\s*(?:\||$)|^\s*(?<d>[^|\n=<>{}\[\]]+?\.(?:jpe?g|png|tiff?|webp))\s*(?:\||$)",
            Ci | RegexOptions.Multiline);
        foreach (Match match in pattern.Matches(wikitext))
        {
            var name = new[] { "a", "b", "c", "d" }.Select(g => match.Groups[g]).First(g => g.Success).Value.Trim();
            name = Regex.Replace(name, @"^(?:File|Image)\s*:\s*", string.Empty, Ci);
            if (name.Length > 0 && !files.Contains(name, StringComparer.OrdinalIgnoreCase)) files.Add(name);
        }

        return files;
    }

    internal enum Quantity
    {
        Length,
        Weight,
        Lifespan
    }

    internal sealed record Section(string Heading, string Text);

    internal sealed record Infobox(
        string? Status,
        string? StatusSystem,
        string? Family,
        string? TaxonomyStart,
        string? RangeMapCaption);
}