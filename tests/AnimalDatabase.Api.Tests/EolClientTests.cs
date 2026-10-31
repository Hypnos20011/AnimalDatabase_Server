using AnimalDatabase.Api.Services;
using System.Net;

namespace AnimalDatabase.Api.Tests;

public class EolClientTests
{
    private static (EolClient Client, FixtureHttpHandler Handler) Create(){
        var handler = new FixtureHttpHandler();
        return (new EolClient(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task Finds_range_text_for_spider_crab()
    {
        var (client, handler) = Create();
        var text = await client.FindRangeTextAsync("Macrocheira kaempferi");
        
        Assert.NotNull(text);
        Assert.Contains("Range mass", text);
    }

    [Fact]
    public async Task Returns_null_when_eol_has_no_match()
    {
        var (client, handler) = Create();
        handler.Override("eol-search/Nonexistus_animalus", "{\"results\":[]}");
        var text = await client.FindRangeTextAsync("Nonexistus animalus");
        
        Assert.Null(text);
    }

    [Fact]
    public async Task Tries_the_next_result_when_the_first_page_is_empty()
    {
        var (client, handler) = Create();
        handler.Override("eol-search/Testus_animalus", """{"results":[{"id":1},{"id":2}]}""");
        handler.Override("eol-page/1", """{"taxonConcept":{"dataObjects":[]}}""");
        handler.Override("eol-page/2",
            """{"taxonConcept":{"dataObjects":[{"description":"<strong>Range mass:</strong> 5 kg"}]}}""");
        
        var text = await client.FindRangeTextAsync("Testus animalus");

        Assert.NotNull(text);
    }

    [Fact]
    public async Task Reads_the_second_texts_page()
    {
        var (client, handler) = Create();
        handler.Override("eol-search/Testus_animalus", """{"results":[{"id":3}]}""");
        handler.Override("eol-page/3", """{"taxonConcept":{"dataObjects":[{"description":"Lives in rivers."}]}}""");
        handler.Override("eol-page/3-p2",
            """{"taxonConcept":{"dataObjects":[{"description":"<strong>Range length:</strong> 40 cm"}]}}""");
        
        var text = await client.FindRangeTextAsync("Testus animalus");
        
        Assert.Contains("Range length", text);
    }
    
    [Fact]
    public async Task Returns_null_when_eol_has_a_server_error()
    {
        var (client, handler) = Create();
        handler.FailNext("eol-search/Macrocheira_kaempferi", HttpStatusCode.InternalServerError);
        
        Assert.Null(await client.FindRangeTextAsync("Macrocheira kaempferi"));
    }

    [Fact]
    public async Task Returns_null_when_eol_times_out()
    {
        var (client, handler) = Create();
        handler.Failure = new TaskCanceledException();
        
        Assert.Null(await client.FindRangeTextAsync("Macrocheira kaempferi"));
    }

    [Fact]
    public async Task Prefers_the_result_with_exact_title()
    {
        var (client, handler) = Create();
        handler.Override("eol-search/Testus_animalus", """
                                                       {"results":[{"id":1, "title":"Testus animalus minor"},{"id":2, "title":"Testus animalus major"},{"id":3,"title":"Testus animalus medius"},{"id":4,"title":"Testus animalus"}]}
                                                       """);
        handler.Override("eol-page/4", """{"taxonConcept":{"dataObjects":[{"description":"<strong>Range mass:</strong> 5 kg"}]}}""");
        
        var text = await client.FindRangeTextAsync("Testus animalus");

        Assert.NotNull(text);
        Assert.DoesNotContain("eol-page/1", handler.RequestedKeys);
    }
}