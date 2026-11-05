using System.Diagnostics;
using System.Globalization;
using System.Net;
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
        if (claims is not null)
        {
            await FillFromWikidataAsync(claims.Value, ancestorsTask.Result, result);
        }

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
            {
                result.IucnStatus = statusName;
            }

            if (result.Family is null)
            {
                result.Family =
                    Truncate(infobox.Family ?? await FindFamilyFromTaxonomyAsync(language, infobox.TaxonomyStart), 120);
            }
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
                        RegexOptions.IgnoreCase))
                {
                    result.RangeRegion = WikipediaText.CleanPhrase(caption);
                }
            }
        }
        else if (result.ScientificName is not null
                 && result.ScientificName.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)
                 && !(preferredName ?? title).Equals(pageTitle, StringComparison.OrdinalIgnoreCase))
        {
            result.CommonName = Capitalize(preferredName ?? title);
        }

        result.ImageUrl = ChooseImage(imageTask.Result, imageInfoTask.Result, wikitext);
        FillImageAttribution(result, imageInfoTask.Result);

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
        if (!latinTitle)
        {
            return result.CommonName;
        }

        var candidates = new List<string>();
        if (wikitext is not null)
        {
            candidates.AddRange(WikipediaText.BoldLeadNames(wikitext));
        }

        if (claims is not null)
        {
            candidates.AddRange(GetEnglishTaxonNames(claims.Value));
        }

        candidates = candidates
            .Where(c => !c.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)
                        && !c.Equals(result.ScientificName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var typedKey = NameKey(typed);
        if (candidates.Any(c => NameKey(c) == typedKey))
        {
            return Capitalize(typed);
        }

        if (candidates.Count > 0)
        {
            return Capitalize(candidates[0]);
        }

        return typed.Equals(pageTitle, StringComparison.OrdinalIgnoreCase) ? result.CommonName : Capitalize(typed);
    }

    private static string NameKey(string name)
    {
        return Regex.Replace(name.ToLowerInvariant().Replace('\u2019', '\''), @"[\s\-]+", " ").Trim();
    }

    private static IEnumerable<string> GetEnglishTaxonNames(JsonElement claims)
    {
        if (!claims.TryGetProperty("P1843", out var list))
        {
            return [];
        }

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
        if (!species && !genus)
        {
            return;
        }

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
        {
            result.IucnStatus = IucnStatuses.TryGetValue(status, out var statusName)
                ? statusName
                : await GetStatusFromLabelAsync(status);
        }

        var weights = GetQuantities(claims, "P2067", MassToKg);
        if (weights.Count > 0)
        {
            result.WeightKgMin = RoundWeight(weights.Min());
            result.WeightKgMax = RoundWeight(weights.Max());
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

    internal static decimal RoundWeight(decimal kilograms)
    {
        var rounded = Math.Round(kilograms, 2, MidpointRounding.AwayFromZero);
        return rounded == 0m && kilograms > 0m ? 0.01m : rounded;
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

    internal static string? ChooseImage(string? pageImage, Dictionary<string, ImageInfo> infos, string? wikitext)
    {
        var leadFile = pageImage is null ? null : FileNameFromUrl(pageImage);
        if (leadFile is null || !infos.TryGetValue(FileKey(leadFile), out var lead) || IsPhoto(lead))
        {
            return pageImage;
        }

        if (wikitext is null)
        {
            return pageImage;
        }

        foreach (var file in WikipediaText.ArticleFiles(wikitext))
        {
            if (infos.TryGetValue(FileKey(file), out var info)
                && info.Mime == "image/jpeg"
                && info.Width >= 500
                && info.Height >= 300
                && IsPhoto(info))
            {
                var url = info.Width > 1280 && info.ThumbUrl is not null ? info.ThumbUrl : info.Url;
                return url?.Split('?')[0] ?? pageImage;
            }
        }

        return pageImage;
    }

    internal static bool IsPhoto(ImageInfo info)
    {
        if (info.Mime is "image/svg+xml" or "image/gif" ||
            info.Title.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var text = $"{info.Title} {info.ObjectName} {info.Description} {info.Categories} {info.Artist}";
        if (Regex.IsMatch(text,
                @"\b(drawings?|drawn|illustrations?|illustrated|plates?|paintings?|painted|engravings?|engraved|lithograph\w*|woodcuts?|watercolou?rs?|diagrams?|maps?|svg|skeletons?|sketch\w*|artwork|line art|clip ?art|lydekker|bloch|museum specimens?|taxidermy|stamps?|scans? of)\b",
                RegexOptions.IgnoreCase))
        {
            return false;
        }

        return !Regex.IsMatch(info.Date ?? string.Empty, @"(?<!\d)(1[0-8]\d\d|19[0-4]\d)(?!\d)")
               && !Regex.IsMatch(info.Description ?? string.Empty,
                   @"\b(?:in|circa|c\.|ca\.|published|printed|dated)\s+(1[0-8]\d\d|19[0-4]\d)(?!\d)",
                   RegexOptions.IgnoreCase);
    }

    internal static void FillImageAttribution(WikipediaImportResult result, Dictionary<string, ImageInfo> infos)
    {
        if (result.ImageUrl is null)
        {
            return;
        }

        var file = FileNameFromUrl(result.ImageUrl);
        if (file is null || !infos.TryGetValue(FileKey(file), out var info))
        {
            return;
        }

        result.Attribution = Truncate(NormalizeSpace(info.Artist), 500);
        var page = info.DescriptionUrl?.Split('?')[0];
        if (page is not null
            && Uri.TryCreate(page, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps)
        {
            result.SourcePageUrl = Truncate(page, 500);
        }
    }

    private static string? NormalizeSpace(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = Regex.Replace(value, @"\s+", " ").Trim();
        return trimmed.Length == 0 ? null : trimmed;
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
        {
            return null;
        }

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

    private async Task<Dictionary<string, ImageInfo>> GetImageInfoAsync(string language, string title)
    {
        var infos = new Dictionary<string, ImageInfo>(StringComparer.OrdinalIgnoreCase);
        using var doc = await TryGetJsonAsync(
            $"https://{language}.wikipedia.org/w/api.php?action=query&generator=images&gimlimit=50&prop=imageinfo&iiprop=url%7Csize%7Cmime%7Cextmetadata&iiurlwidth=1280&iiextmetadatafilter=ImageDescription%7CObjectName%7CCategories%7CDateTimeOriginal%7CArtist&iiextmetadatalanguage=en&format=json&formatversion=2&titles={Uri.EscapeDataString(title)}");
        if (doc is null
            || !doc.RootElement.TryGetProperty("query", out var query)
            || !query.TryGetProperty("pages", out var pages))
        {
            return infos;
        }

        foreach (var page in pages.EnumerateArray())
        {
            var fileTitle = GetString(page, "title");
            if (fileTitle is null
                || !page.TryGetProperty("imageinfo", out var list)
                || list.ValueKind != JsonValueKind.Array
                || list.GetArrayLength() == 0)
            {
                continue;
            }

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
                MetaValue(meta, "DateTimeOriginal"),
                GetString(ii, "descriptionurl"));
        }

        return infos;
    }

    private static string? MetaValue(JsonElement meta, string name)
    {
        if (meta.ValueKind != JsonValueKind.Object || !meta.TryGetProperty(name, out var entry))
        {
            return null;
        }

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
        {
            return null;
        }

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
        {
            return null;
        }

        return GetString(main, "content");
    }

    private async Task<string?> FindFamilyFromTaxonomyAsync(string language, string? start)
    {
        var name = start;
        for (var hop = 0; hop < 5 && !string.IsNullOrWhiteSpace(name); hop++)
        {
            var template = await GetWikitextAsync(language, $"Template:Taxonomy/{name}");
            if (template is null)
            {
                return null;
            }

            var (rank, link, parent) = WikipediaText.ReadTaxonomyTemplate(template);
            if (rank is null)
            {
                return null;
            }

            if (rank.Equals("familia", StringComparison.OrdinalIgnoreCase))
            {
                return link is not null && Regex.IsMatch(link, @"^[A-Z][a-z]+idae$") ? link : null;
            }

            if (!BelowFamilyRanks.Contains(rank, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

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
        {
            return null;
        }

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
                {
                    throw new HttpRequestException($"Wikipedia answered {(int)response.StatusCode}", null,
                        response.StatusCode);
                }

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
                if (decimal.TryParse(GetString(value, key), NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var number)
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
        string? Date,
        string? DescriptionUrl = null);
}
