using System.Net;
using System.Text.Json.Nodes;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class WikipediaFieldTests
{
    private static string Fixture(string key) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", key.Replace('/', Path.DirectorySeparatorChar) + ".json"));

    [Fact]
    public async Task Glass_frog_with_a_latin_title_gets_the_bold_common_name()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportAsync("en", "Hyalinobatrachium fleischmanni");

        Assert.NotNull(result);
        Assert.Equal("Fleischmann's glass frog", result.CommonName);
        Assert.Equal("Hyalinobatrachium fleischmanni", result.ScientificName);
        Assert.Equal("Centrolenidae", result.Family);
        Assert.Equal("amphibian", result.TaxonomicClass);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Glass_frog_keeps_the_typed_name_when_it_matches_a_common_name()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("fleischmann's glass frog");

        Assert.NotNull(result);
        Assert.Equal("Fleischmann's glass frog", result.CommonName);
        Assert.Equal<decimal?>(19m, result.LengthMmMin);
        Assert.Equal<decimal?>(32m, result.LengthMmMax);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Blobfish_drawing_is_kept_when_the_article_has_no_photo()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("blobfish");

        Assert.NotNull(result);
        Assert.EndsWith("Psychrolutes_marcidus.jpg", result.ImageUrl);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Mata_mata_status_comes_from_the_infobox_when_wikidata_has_none()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("mata mata");

        Assert.NotNull(result);
        Assert.Equal("Chelus fimbriatus", result.ScientificName);
        Assert.Equal("least concern", result.IucnStatus);
        Assert.Equal("reptile", result.TaxonomicClass);
        Assert.Equal("Chelidae", result.Family);
        Assert.Null(result.LengthMmMin);
        Assert.Equal<decimal?>(450m, result.LengthMmMax);
        Assert.Equal<decimal?>(17200m, result.WeightGramsMin);
        Assert.Equal<decimal?>(17200m, result.WeightGramsMax);
        Assert.Equal("Amazon basin and river system of the eastern Guianas", result.RangeRegion);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Infobox_status_outside_IUCN_3_1_is_ignored()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override("revisions/Mata_mata", Fixture("revisions/Mata_mata").Replace("IUCN3.1", "CITES"));

        var result = await importer.ImportByNameAsync("mata mata");

        Assert.NotNull(result);
        Assert.Null(result.IucnStatus);
    }

    [Fact]
    public async Task Family_falls_back_to_the_taxonomy_templates_when_wikidata_has_none()
    {
        var (importer, handler) = TestImporter.Create();
        var sparql = JsonNode.Parse(Fixture("sparql/Q317386"))!;
        var bindings = sparql["results"]!["bindings"]!.AsArray();
        foreach (var binding in bindings.Where(b => b!["rank"]?["value"]?.GetValue<string>().EndsWith("/Q35409") == true).ToList())
        {
            bindings.Remove(binding);
        }
        handler.Override("sparql/Q317386", sparql.ToJsonString());
        handler.Override(
            "revisions/Template_Taxonomy/Chelus",
            """{"query":{"pages":[{"revisions":[{"slots":{"main":{"content":"{{Don't edit this line {{{machine code|}}}\n|rank=genus\n|link=Chelus\n|parent=Chelinae\n}}"}}}]}]}}""");
        handler.Override(
            "revisions/Template_Taxonomy/Chelinae",
            """{"query":{"pages":[{"revisions":[{"slots":{"main":{"content":"{{Don't edit this line {{{machine code|}}}\n|rank=subfamilia\n|link=Chelinae\n|parent=Chelidae\n}}"}}}]}]}}""");
        handler.Override(
            "revisions/Template_Taxonomy/Chelidae",
            """{"query":{"pages":[{"revisions":[{"slots":{"main":{"content":"{{Don't edit this line {{{machine code|}}}\n|rank=familia\n|link=Chelidae\n|parent=Pleurodira\n}}"}}}]}]}}""");

        var result = await importer.ImportByNameAsync("mata mata");

        Assert.NotNull(result);
        Assert.Equal("Chelidae", result.Family);
        Assert.Contains("revisions/Template_Taxonomy/Chelidae", handler.RequestedKeys);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Shoebill_sizes_come_from_the_article_text()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("shoebill");

        Assert.NotNull(result);
        Assert.Equal("Balaeniceps rex", result.ScientificName);
        Assert.Equal("bird", result.TaxonomicClass);
        Assert.Equal<decimal?>(1000m, result.LengthMmMin);
        Assert.Equal<decimal?>(1400m, result.LengthMmMax);
        Assert.Equal<decimal?>(4000m, result.WeightGramsMin);
        Assert.Equal<decimal?>(7000m, result.WeightGramsMax);
        Assert.Equal("Freshwater swamps", result.Habitat);
        Assert.Equal("Central tropical Africa", result.RangeRegion);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Aye_aye_length_skips_the_tail_and_keeps_the_body()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("aye-aye");

        Assert.NotNull(result);
        Assert.Equal<decimal?>(360m, result.LengthMmMin);
        Assert.Equal<decimal?>(430m, result.LengthMmMax);
        Assert.Equal("Rainforest or dry deciduous forest", result.Habitat);
        Assert.Equal("East coast of Madagascar", result.RangeRegion);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Star_nosed_mole_gets_a_habitat_and_a_range_but_no_guessed_size()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("star-nosed mole");

        Assert.NotNull(result);
        Assert.Equal("Moist, low elevation areas", result.Habitat);
        Assert.Equal("Northeastern parts of North America", result.RangeRegion);
        Assert.Contains("fastest-eating mammal", result.Profile);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Naked_mole_rat_fun_fact_is_a_scored_sentence_not_from_the_profile_lead()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("naked mole-rat");

        Assert.NotNull(result);
        var parts = result.Profile!.Split("\n\nFun fact: ");
        Assert.Equal(2, parts.Length);
        Assert.StartsWith("The naked mole-rat", parts[0]);
        Assert.Contains("longer than any other rodent", parts[1]);
        Assert.DoesNotContain(parts[1], parts[0]);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task Higher_taxa_get_no_sizes_from_the_article_text()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("octopus");

        Assert.NotNull(result);
        Assert.Null(result.WeightGramsMin);
        Assert.Null(result.WeightGramsMax);
        Assert.Null(result.LengthMmMax);
        Assert.Empty(handler.MissingKeys);
    }

    [Fact]
    public async Task A_server_error_is_retried_once()
    {
        var (importer, handler) = TestImporter.Create();
        handler.FailNext("summary/Okapi", HttpStatusCode.ServiceUnavailable);

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("Okapia johnstoni", result.ScientificName);
        Assert.Equal(2, handler.RequestedKeys.Count(k => k == "summary/Okapi"));
    }

    [Fact]
    public async Task A_second_server_error_is_not_retried_again()
    {
        var (importer, handler) = TestImporter.Create();
        handler.FailNext("summary/Okapi", HttpStatusCode.BadGateway);
        handler.FailNext("summary/Okapi", HttpStatusCode.BadGateway);

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => importer.ImportByNameAsync("okapi"));
        Assert.Equal(2, handler.RequestedKeys.Count(k => k == "summary/Okapi"));
    }

    [Fact]
    public async Task A_failing_optional_request_still_gives_a_result()
    {
        var (importer, handler) = TestImporter.Create();
        handler.FailNext("revisions/Okapi", HttpStatusCode.InternalServerError);
        handler.FailNext("revisions/Okapi", HttpStatusCode.InternalServerError);

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("endangered", result.IucnStatus);
        Assert.Equal<decimal?>(200000m, result.WeightGramsMin);
    }

    [Fact]
    public async Task A_client_error_is_not_retried()
    {
        var (importer, handler) = TestImporter.Create();

        await importer.ImportAsync("en", "No such animal page");

        Assert.Single(handler.RequestedKeys, k => k == "summary/No_such_animal_page");
    }

    [Fact]
    public void A_drawing_lead_image_is_replaced_by_the_first_article_photo()
    {
        var drawing = new WikipediaImporter.ImageInfo("Fish drawing.jpg", "image/jpeg", 1000, 400, "https://upload.wikimedia.org/wikipedia/commons/a/ab/Fish_drawing.jpg", null, "Fish", "Drawing by Bloch, 1790", "Fish illustrations", "Bloch", "1790");
        var map = new WikipediaImporter.ImageInfo("Fish map.png", "image/png", 800, 600, "https://upload.wikimedia.org/wikipedia/commons/c/cd/Fish_map.png", null, null, "Range map", "Maps", null, "2020");
        var photo = new WikipediaImporter.ImageInfo("Fish photo.jpg", "image/jpeg", 4000, 3000, "https://upload.wikimedia.org/wikipedia/commons/e/ef/Fish_photo.jpg", "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/Fish_photo.jpg/1280px-Fish_photo.jpg?x=1", null, "A fish in an aquarium", "Photographs of fish", "Someone", "2019-05-01");
        var infos = new Dictionary<string, WikipediaImporter.ImageInfo>
        {
            [drawing.Title] = drawing,
            [map.Title] = map,
            [photo.Title] = photo,
        };
        const string wikitext = "{{Speciesbox\n| image = Fish_drawing.jpg\n| range_map = Fish map.png\n}}\n[[File:Fish photo.jpg|thumb|A fish]]";

        var chosen = WikipediaImporter.ChooseImage("https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Fish_drawing.jpg/1280px-Fish_drawing.jpg", infos, wikitext);

        Assert.Equal("https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/Fish_photo.jpg/1280px-Fish_photo.jpg", chosen);
    }

    [Fact]
    public void A_taxon_authority_year_does_not_make_a_photo_old()
    {
        var info = new WikipediaImporter.ImageInfo("Okapia johnstoni 17zz.jpg", "image/jpeg", 1000, 1348, null, null, null, "Location taken: Miami MetroZoo. Names: Okapia johnstoni (P. L. Sclater, 1901)", "Okapia johnstoni in Zoo Miami", null, "2007-02-17");

        Assert.True(WikipediaImporter.IsPhoto(info));
    }

    [Fact]
    public void A_photo_lead_image_is_kept()
    {
        var photo = new WikipediaImporter.ImageInfo("Okapi.jpg", "image/jpeg", 2000, 1500, "https://upload.wikimedia.org/wikipedia/commons/1/12/Okapi.jpg", null, null, "Okapi at a zoo", "Okapia johnstoni in zoos", "Someone", "2012-01-01");
        var infos = new Dictionary<string, WikipediaImporter.ImageInfo> { [photo.Title] = photo };
        const string lead = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/Okapi.jpg/1280px-Okapi.jpg";

        Assert.Equal(lead, WikipediaImporter.ChooseImage(lead, infos, "[[File:Other.jpg]]"));
    }

    [Theory]
    [InlineData("Lydekker plate.jpg", "image/jpeg", "Plate from Lydekker", "")]
    [InlineData("Old fish.jpg", "image/jpeg", "A fish", "1897")]
    [InlineData("Old fish 2.jpg", "image/jpeg", "Photographed in 1921", "")]
    [InlineData("Skeleton.jpg", "image/jpeg", "Skeleton of a fish", "2010")]
    [InlineData("Fish.svg", "image/svg+xml", "A fish", "2010")]
    public void Non_photos_are_detected(string title, string mime, string description, string date)
    {
        var info = new WikipediaImporter.ImageInfo(title, mime, 1000, 800, null, null, null, description, null, null, date);

        Assert.False(WikipediaImporter.IsPhoto(info));
    }

    [Fact]
    public async Task Blue_ringed_octopus_genus_gets_lifespan_habitat_and_range_but_no_sizes()
    {
        var (importer, handler) = TestImporter.Create();

        var result = await importer.ImportByNameAsync("blue-ringed octopus");

        Assert.NotNull(result);
        Assert.Equal("Hapalochlaena", result.ScientificName);
        Assert.Equal(24m, result.LifespanMonthsMin);
        Assert.Equal(36m, result.LifespanMonthsMax);
        Assert.Equal("Tide pools and coral reefs", result.Habitat);
        Assert.Equal("Pacific and Indian Oceans, from Japan to Australia", result.RangeRegion);
        Assert.Null(result.LengthMmMin);
        Assert.Null(result.LengthMmMax);
        Assert.Null(result.WeightGramsMin);
        Assert.Null(result.WeightGramsMax);
        Assert.Empty(handler.MissingKeys);
    }
}
