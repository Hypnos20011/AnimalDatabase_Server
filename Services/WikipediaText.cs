using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimalDatabase.Api.Services;

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
            {
                return sections.Where(s => s.Text.Length > 0 || s.Heading.Length == 0).ToList();
            }
        }

        sections.Add(new Section(heading, string.Join("\n", buffer).Trim()));
        return sections.Where(s => s.Text.Length > 0 || s.Heading.Length == 0).ToList();
    }

    internal static List<string> Sentences(string? text)
    {
        var sentences = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return sentences;
        }

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
                {
                    continue;
                }
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

            if (depth == 0)
            {
                builder.Append(text[i]);
            }
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
            if (!allowed)
            {
                continue;
            }

            var sentences = Sentences(section.Text);
            for (var i = 0; i < sentences.Count; i++)
            {
                var values = MeasureSentence(sentences[i], quantity);
                if (values.Count == 0)
                {
                    continue;
                }

                var male = Regex.IsMatch(sentences[i], @"\bmales?\b", Ci);
                var female = Regex.IsMatch(sentences[i], @"\bfemales?\b", Ci);
                if (male != female && i + 1 < sentences.Count)
                {
                    var other = female ? @"\bmales?\b" : @"\bfemales?\b";
                    if (Regex.IsMatch(sentences[i + 1], other, Ci))
                    {
                        values.AddRange(MeasureSentence(sentences[i + 1], quantity));
                    }
                }

                var mins = values.Where(v => v.Min is not null).Select(v => v.Min!.Value).ToList();
                var maxes = values.Where(v => v.Max is not null).Select(v => v.Max!.Value).ToList();
                decimal? min = mins.Count > 0 ? mins.Min() : null;
                decimal? max = maxes.Count > 0 ? maxes.Max() : null;
                if (min is not null && max is not null && min > max)
                {
                    continue;
                }

                return (min, max);
            }
        }

        return null;
    }

    internal static List<(decimal? Min, decimal? Max)> MeasureSentence(string sentence, Quantity quantity)
    {
        var found = new List<(decimal? Min, decimal? Max)>();
        if (BirthWords.IsMatch(sentence))
        {
            return found;
        }

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
        {
            text = NumberWordYears.Replace(text,
                m => NumberWords[m.Groups["w"].Value].ToString(CultureInfo.InvariantCulture));
        }

        var clauses = new List<string>();
        foreach (var piece in Regex.Split(text, @";|,\s+|\s+[—–]\s+|—"))
        {
            if (clauses.Count > 0 && Regex.IsMatch(piece, @"^\s*(?:which|who|such as|that|including)\b", Ci))
            {
                clauses[^1] = $"{clauses[^1]}, {piece}";
            }
            else
            {
                clauses.Add(piece);
            }
        }

        var firstClause = clauses.Count > 0 ? clauses[0] : text;
        foreach (var clause in clauses)
        {
            if (OutlierClause.IsMatch(clause))
            {
                continue;
            }

            var matches = MeasurementPattern.Matches(clause).ToList();
            for (var m = 0; m < matches.Count; m++)
            {
                var match = matches[m];
                var unit = match.Groups["u"].Value.ToLowerInvariant();
                var type = UnitType(unit);
                if (type != quantity)
                {
                    continue;
                }

                if (match.Groups["ua"].Success &&
                    UnitType(match.Groups["ua"].Value.ToLowerInvariant()) != type)
                {
                    continue;
                }

                if (match.Groups["ua"].Success
                    && !match.Groups["ua"].Value.Equals(match.Groups["u"].Value, StringComparison.OrdinalIgnoreCase)
                    && Factor(match.Groups["ua"].Value.ToLowerInvariant()) != Factor(unit))
                {
                    continue;
                }

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

                if (MaturityWords.IsMatch(context) || ExcludedContext.IsMatch(context))
                {
                    continue;
                }

                if (quantity == Quantity.Length)
                {
                    var whole = context.Contains("bodylength", StringComparison.OrdinalIgnoreCase);
                    if (!LengthWords.IsMatch(context))
                    {
                        continue;
                    }

                    if (HeightWords.IsMatch(context) &&
                        !Regex.IsMatch(context, @"\b(?:long|length|bodylength)\b", Ci))
                    {
                        continue;
                    }

                    if (!whole && ExcludedContext.IsMatch(text))
                    {
                        continue;
                    }
                }
                else if (quantity == Quantity.Weight)
                {
                    if (!WeightWords.IsMatch(context) || ExcludedContext.IsMatch(firstClause))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!LifespanWords.IsMatch(context) || LifespanExclusions.IsMatch(context))
                    {
                        continue;
                    }
                }

                if (!TryNumber(match.Groups["a"].Value, out var a))
                {
                    continue;
                }

                decimal? b = null;
                if (match.Groups["b"].Success)
                {
                    if (!TryNumber(match.Groups["b"].Value, out var parsed) || parsed < a)
                    {
                        continue;
                    }

                    b = parsed;
                }

                var factor = Factor(unit);
                var low = a * factor;
                var high = (b ?? a) * factor;
                if (!InBounds(low, quantity) || !InBounds(high, quantity))
                {
                    continue;
                }

                if (b is null && (MaxOnlyPrefix.IsMatch(before) || NegatedMinPrefix.IsMatch(before)))
                {
                    found.Add((null, high));
                }
                else if (b is null && MinOnlyPrefix.IsMatch(before))
                {
                    found.Add((low, null));
                }
                else
                {
                    found.Add((low, high));
                }
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
            Quantity.Weight => value is >= 0.0005m and <= 9999999.99m,
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
                    Ci))
            {
                continue;
            }

            var text = Regex.Replace(sentence, @"\s*\([^()]*\)", string.Empty);
            if (range is null)
            {
                var match = Regex.Match(text, @"\b(?:native|endemic|restricted|confined)\s+(?:only\s+)?to\s+(?<x>.+)",
                    Ci);
                if (!match.Success)
                {
                    match = Regex.Match(text,
                        @"\b(?:found|distributed|occurs?|ranges?|lives?)\s+(?:(?:mainly|mostly|primarily|largely|predominantly|only|exclusively|widely)\s+)?(?:only\s+)?(?<p>in|throughout|across|along|on)\s+(?<x>.+)",
                        Ci);
                }

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
                    if (habitat is not null && tail is not null && range is null)
                    {
                        range = CleanRange(tail);
                    }
                }
            }

            if (habitat is not null && range is not null)
            {
                break;
            }
        }

        return (habitat, range);
    }

    private static (string? Habitat, string Range)? FindGeographicTail(string phrase)
    {
        var clause = Regex.Split(phrase,
            @";|:|\.\s|\.$|,\s*(?:where|which|but|although|though|while|whereas|and|it|they)\b|\s+(?:where|which|that|because|although|though|while|whereas|but)\s",
            Ci)[0];
        var match = GeographicTail.Match(clause);
        if (!match.Success)
        {
            return null;
        }

        var range = CleanRange(match.Groups["t"].Value + match.Groups["ft"].Value);
        if (range is null || !Regex.IsMatch(match.Groups["t"].Value, @"^\p{Lu}"))
        {
            return null;
        }

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
        {
            return (match.Groups["h"].Value, match.Groups["t"].Value);
        }

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
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return null;
        }

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
        {
            return null;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    internal static string? ProfileFromLead(string? extract)
    {
        if (string.IsNullOrWhiteSpace(extract))
        {
            return null;
        }

        var cleaned = RemovePronunciation(extract);
        var sentences = Sentences(cleaned);
        if (sentences.Count == 0)
        {
            return null;
        }

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
            if (section.Heading.Length > 0 && FunFactExcludedSection.IsMatch(section.Heading))
            {
                continue;
            }

            foreach (var raw in Sentences(RemovePronunciation(section.Text)))
            {
                var sentence = raw.Trim();
                if (!sentence.EndsWith('.') || !Regex.IsMatch(sentence, @"^[\p{Lu}""“]"))
                {
                    continue;
                }

                if (profileSentences.Any(p => p.Equals(sentence, StringComparison.Ordinal))
                    || (profile is not null && profile.Contains(sentence, StringComparison.Ordinal)))
                {
                    continue;
                }

                var words = Words(sentence);
                if (words.Count > 0 && profileWords.Count > 0
                                    && words.Count(w => profileWords.Contains(w)) >= words.Count * 0.5)
                {
                    continue;
                }

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
                Ci))
        {
            score += 3;
        }

        if (Regex.IsMatch(sentence,
                @"\b(?:can|able to|capable of|regenerat\w*|survives?|surviving|without|never|despite)\b",
                Ci))
        {
            score += 2;
        }

        if (Regex.IsMatch(sentence, @"\d"))
        {
            score += 1;
        }

        if (Regex.IsMatch(sentence,
                @"\b(?:venom\w*|poison\w*|toxi\w*|glow\w*|biolumin\w*|transparent|translucent|see-through|camouflag\w*|mimic\w*|hibernat\w*|torpor|backwards?|upside[\s-]down|blood|mucus|slime|cannibal\w*|electr\w*|echolocat\w*|tools?|sings?|songs?|clatter\w*|clap\w*|danc\w*|play(?:s|ing)? dead|inflat\w*|spit\w*|squirt\w*|ink|pain|cancer|oxygen|immune|sleep\w*|swallow\w*|hears?|smells?|underwater|pheromone\w*|queen|eusocial|cold-blooded|regrow\w*|limbs?|neoten\w*|gills?|eat\w*|consum\w*|tap\w*|drink\w*|walks?)\b",
                Ci))
        {
            score += 2;
        }

        if (Regex.IsMatch(sentence,
                @"\b(?:genus|genera|species was|described by|first described|named after|named for|etymolog\w*|derived from|latin|greek|taxonom\w*|classified|subspecies|phylogen\w*|clade|sister group|synonym\w*|binomial|family\b)",
                Ci))
        {
            score -= 4;
        }

        if (Regex.IsMatch(sentence,
                @"\b(?:IUCN|Red List|Least Concern|assessed|endangered|threatened|vulnerable|conservation)\b",
                Ci))
        {
            score -= 4;
        }

        if (Regex.IsMatch(sentence, @"\b(?:\d+(?:\.\d+)?\s*(?:mm|cm|m|kg|g|in|ft|lb))\b") && !Regex.IsMatch(sentence,
                @"\b(?:largest|smallest|longest|fastest|heaviest|deepest|the only|world's|unlike)\b", Ci))
        {
            score -= 2;
        }

        if (Regex.IsMatch(sentence, @"\b(?:prior to|was previously|formerly|until the discovery)\b", Ci))
        {
            score -= 3;
        }

        if (Regex.IsMatch(sentence, @"^(?:Like most|Like other|As with|As in)\b", Ci))
        {
            score -= 2;
        }

        if (Regex.IsMatch(sentence, @"\b(?:study|studies|researchers?|reported|report|published|journal)\b",
                Ci))
        {
            score -= 1;
        }

        if (Regex.IsMatch(sentence, @"\b(?:1[5-9]\d\d|20\d\d)\b"))
        {
            score -= 1;
        }

        if (sentence.Length < 60 || sentence.Length > 220)
        {
            score -= 3;
        }

        if (Regex.IsMatch(sentence,
                @"^(?:However|Further|Furthermore|Also|Additionally|Moreover|In addition|Another|This|These|It is also)\b"))
        {
            score -= 1;
        }

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
        if (!start.Success)
        {
            return new Infobox(null, null, null, null, null);
        }

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
        {
            taxonomyStart = Get("genus") ?? Get("parent") ??
                Get("taxon")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        else if (box is "automatic taxobox")
        {
            taxonomyStart = Get("taxon");
        }

        if (taxonomyStart is not null)
        {
            taxonomyStart = StripCitations(taxonomyStart).Trim();
            if (!Regex.IsMatch(taxonomyStart, @"^[A-Z][a-z]+(?: \([A-Z][a-z]+\))?$"))
            {
                taxonomyStart = null;
            }
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
        {
            parameters.TryAdd(match.Groups["k"].Value, match.Groups["v"].Value.Trim());
        }

        string? Get(string name)
        {
            return parameters.TryGetValue(name, out var v) && v.Length > 0 ? v : null;
        }

        var link = Get("link");
        if (link is not null)
        {
            link = Regex.Replace(StripCitations(link), @"\s+", " ").Trim();
        }

        var parent = Get("parent");
        if (parent is not null)
        {
            parent = parent.Split('/')[0].Trim();
        }

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
                if (depth > 1)
                {
                    current.Append(two);
                }

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
            if (index <= 0)
            {
                continue;
            }

            var key = part[..index].Trim();
            if (key.Length > 0 && !parameters.ContainsKey(key))
            {
                parameters[key] = part[(index + 1)..];
            }
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
            if (!match.Groups["bold"].Success)
            {
                continue;
            }

            var raw = match.Groups["bold"].Value;
            if (raw.StartsWith("''", StringComparison.Ordinal) ||
                raw.Contains("''", StringComparison.Ordinal))
            {
                continue;
            }

            var name = Regex.Replace(StripCitations(raw), @"\s+", " ").Trim();
            if (name.Length is >= 2 and <= 80 && Regex.IsMatch(name, @"^[\p{L}][\p{L}\s'’\-.]*$"))
            {
                names.Add(name);
            }
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
            if (name.Length > 0 && !files.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                files.Add(name);
            }
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