using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class WikipediaImporterTests
{
    [Fact]
    public async Task Okapi_by_name_is_an_endangered_mammal_with_canonical_measurements()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("Okapi", result.CommonName);
        Assert.Equal("Okapia johnstoni", result.ScientificName);
        Assert.Equal("Giraffidae", result.Family);
        Assert.Equal("mammal", result.TaxonomicClass);
        Assert.Equal("endangered", result.IucnStatus);
        Assert.Equal<decimal?>(200000m, result.WeightGramsMin);
        Assert.Equal<decimal?>(300000m, result.WeightGramsMax);
        Assert.Equal<decimal?>(2500m, result.LengthMmMin);
        Assert.Equal<decimal?>(2500m, result.LengthMmMax);
        Assert.Equal<decimal?>(240m, result.LifespanMonthsMin);
        Assert.Equal<decimal?>(360m, result.LifespanMonthsMax);
        Assert.Equal("Democratic Republic of the Congo", result.RangeRegion);
        Assert.Equal("https://en.wikipedia.org/wiki/Okapi", result.SourceUrl);
        Assert.StartsWith("https://thumb.wikimedia.org/", result.ImageUrl);
        Assert.DoesNotContain("?", result.ImageUrl);
        Assert.StartsWith("The okapi", result.Profile);
        Assert.Contains("\n\nFun fact: ", result.Profile);
        Assert.Contains("labels/Q96377276", handler.RequestedKeys);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Okapi_by_wikipedia_link_gives_the_same_result_without_searching()
    {
        var (importer, handler) = TestImporter.Create();

        Assert.True(WikipediaImporter.TryParseUrl("https://en.wikipedia.org/wiki/Okapi", out var language, out var title));
        var result = await importer.ImportAsync(language, title);

        Assert.NotNull(result);
        Assert.Equal("Okapi", result.CommonName);
        Assert.Equal("mammal", result.TaxonomicClass);
        Assert.Equal("endangered", result.IucnStatus);
        Assert.Equal<decimal?>(200000m, result.WeightGramsMin);
        Assert.Equal<decimal?>(300000m, result.WeightGramsMax);
        Assert.DoesNotContain(handler.RequestedKeys, k => k.StartsWith("search/"));
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Aye_aye_uses_the_adult_weight_and_ignores_the_birth_weight()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("aye-aye");

        Assert.NotNull(result);
        Assert.Equal("Aye-aye", result.CommonName);
        Assert.Equal("Daubentonia madagascariensis", result.ScientificName);
        Assert.Equal("Daubentoniidae", result.Family);
        Assert.Equal("mammal", result.TaxonomicClass);
        Assert.Equal("endangered", result.IucnStatus);
        Assert.Equal<decimal?>(2500m, result.WeightGramsMin);
        Assert.Equal<decimal?>(2500m, result.WeightGramsMax);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Blobfish_is_a_fish_without_status_and_keeps_the_typed_name()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("blobfish");

        Assert.NotNull(result);
        Assert.Equal("Blobfish", result.CommonName);
        Assert.Equal("Psychrolutes marcidus", result.ScientificName);
        Assert.Equal("Psychrolutidae", result.Family);
        Assert.Equal("fish", result.TaxonomicClass);
        Assert.Null(result.IucnStatus);
        Assert.Null(result.WeightGramsMin);
        Assert.Equal("https://en.wikipedia.org/wiki/Psychrolutes_marcidus", result.SourceUrl);
        Assert.DoesNotContain(handler.RequestedKeys, k => k.StartsWith("labels/"));
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Tardigrade_is_an_invertebrate()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("tardigrade");

        Assert.NotNull(result);
        Assert.Equal("Tardigrade", result.CommonName);
        Assert.Equal("Tardigrada", result.ScientificName);
        Assert.Equal("invertebrate", result.TaxonomicClass);
        Assert.Null(result.Family);
        Assert.Null(result.IucnStatus);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Tardigrade_length_of_half_a_millimetre_keeps_canonical_precision()
    {
        var (importer, _) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("tardigrade");

        Assert.NotNull(result);
        Assert.Equal<decimal?>(0.5m, result.LengthMmMin);
        Assert.Equal<decimal?>(0.5m, result.LengthMmMax);
    }

    [Fact]
    public async Task Octopus_is_an_invertebrate()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("octopus");

        Assert.NotNull(result);
        Assert.Equal("Octopus", result.CommonName);
        Assert.Equal("Octopoda", result.ScientificName);
        Assert.Equal("invertebrate", result.TaxonomicClass);
        Assert.Null(result.Family);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Unknown_name_returns_null()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("qwxzvbnmplkjhgf animal");

        Assert.Null(result);
        Assert.Equal(new[] { "search/qwxzvbnmplkjhgf_animal" }, handler.RequestedKeys);
    }

    [Fact]
    public async Task Missing_article_returns_null()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportAsync("en", "No such animal page");

        Assert.Null(result);
        Assert.Equal(new[] { "summary/No_such_animal_page" }, handler.MissingKeys);
    }

    [Fact]
    public async Task Disambiguation_article_returns_null()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override("summary/Mercury", """{"type":"disambiguation","title":"Mercury"}""");

        var result = await importer.ImportAsync("en", "Mercury");

        Assert.Null(result);
        Assert.Equal(new[] { "summary/Mercury" }, handler.RequestedKeys);
    }

    [Fact]
    public async Task Search_skips_disambiguation_pages()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override(
            "search/mercury",
            """{"query":{"pages":[{"title":"Mercury","index":1,"pageprops":{"disambiguation":""}},{"title":"Okapi","index":2}]}}""");

        var result = await importer.ImportByNameAsync("mercury");

        Assert.NotNull(result);
        Assert.Equal("Okapi", result.CommonName);
        Assert.DoesNotContain("summary/Mercury", handler.RequestedKeys);
    }

    [Fact]
    public async Task Status_label_with_species_suffix_is_mapped()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override(
            "labels/Q96377276",
            """{"entities":{"Q96377276":{"id":"Q96377276","labels":{"en":{"language":"en","value":"Endangered species"}}}}}""");

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("endangered", result.IucnStatus);
    }

    [Fact]
    public async Task Unknown_status_label_falls_back_to_the_infobox_status()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override(
            "labels/Q96377276",
            """{"entities":{"Q96377276":{"id":"Q96377276","labels":{"en":{"language":"en","value":"Lower risk"}}}}}""");

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("endangered", result.IucnStatus);
        Assert.Equal("mammal", result.TaxonomicClass);
    }

    [Theory]
    [InlineData("mammal", "Animalia", "Chordata", "Vertebrata", "Sarcopterygii", "Tetrapoda", "Mammalia")]
    [InlineData("bird", "Animalia", "Vertebrata", "Sauropsida", "Aves")]
    [InlineData("reptile", "Animalia", "Vertebrata", "Sauropsida", "Squamata")]
    [InlineData("amphibian", "Animalia", "Vertebrata", "Sarcopterygii", "Tetrapoda", "Amphibia")]
    [InlineData("fish", "Animalia", "Vertebrata", "Osteichthyes", "Actinopterygii", "Teleostei")]
    [InlineData("fish", "Animalia", "Vertebrata", "Chondrichthyes", "Elasmobranchii")]
    [InlineData("fish", "Animalia", "Vertebrata", "Osteichthyes", "Sarcopterygii")]
    [InlineData("invertebrate", "Animalia", "Mollusca", "Cephalopoda")]
    [InlineData("invertebrate", "Animalia", "Tardigrada")]
    [InlineData("mammal", "animalia", "vertebrata", "mammalia")]
    public void ClassifyTaxa_picks_the_first_matching_class(string expected, params string[] taxa)
    {
        var result = WikipediaImporter.ClassifyTaxa(new HashSet<string>(taxa, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Animalia", "Vertebrata", "Sarcopterygii", "Tetrapoda")]
    [InlineData("Plantae", "Magnoliophyta")]
    [InlineData("Fungi")]
    public void ClassifyTaxa_returns_null_when_no_rule_matches(params string[] taxa)
    {
        var result = WikipediaImporter.ClassifyTaxa(new HashSet<string>(taxa, StringComparer.OrdinalIgnoreCase));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("https://en.wikipedia.org/wiki/Okapi", "en", "Okapi")]
    [InlineData("https://de.wikipedia.org/wiki/Okapi", "de", "Okapi")]
    [InlineData("https://en.m.wikipedia.org/wiki/Aye-aye", "en", "Aye-aye")]
    [InlineData("https://www.wikipedia.org/wiki/Okapi", "en", "Okapi")]
    [InlineData("https://en.wikipedia.org/wiki/Giant_huntsman_spider", "en", "Giant huntsman spider")]
    [InlineData("https://de.wikipedia.org/wiki/K%C3%A4nguru", "de", "Känguru")]
    public void TryParseUrl_accepts_article_links(string url, string expectedLanguage, string expectedTitle)
    {
        Assert.True(WikipediaImporter.TryParseUrl(url, out var language, out var title));
        Assert.Equal(expectedLanguage, language);
        Assert.Equal(expectedTitle, title);
    }

    [Theory]
    [InlineData("okapi")]
    [InlineData("https://en.wikipedia.org/w/index.php?title=Okapi")]
    [InlineData("https://example.org/wiki/Okapi")]
    [InlineData("https://en.wikipedia.org/wiki/")]
    public void TryParseUrl_rejects_other_input(string input)
    {
        Assert.False(WikipediaImporter.TryParseUrl(input, out _, out _));
    }
}
