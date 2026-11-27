using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;
using AnimalDatabase.Api.Services;
using System.Net;

namespace AnimalDatabase.Api.Tests;

public class WikipediaControllerTests
{
    private static (WikipediaController Controller, FixtureHttpHandler Handler) Create()
    {
        var (importer, handler) = TestImporter.Create();
        var anAge = new AnAgeSource(Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "anage_data.txt")); 
        var sealife = new SeaLifeBaseSource( 
            Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "sealifebase_data.txt"), 
            Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "fishbase_data.txt")); 
        var eol = new EolClient(new HttpClient(handler));
        
        return (new WikipediaController(importer, anAge, sealife, eol), handler);
    }

    [Fact]
    public async Task Plain_name_returns_ok()
    {
        var (controller, handler) = Create();

        var response = await controller.Import("okapi");

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<WikipediaImportResult>(ok.Value);
        Assert.Equal("Okapi", result.CommonName);
        Assert.Contains("search/okapi", handler.RequestedKeys);
    }

    [Fact]
    public async Task Wikipedia_link_returns_ok_without_searching()
    {
        var (controller, handler) = Create();

        var response = await controller.Import("https://en.wikipedia.org/wiki/Aye-aye");

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<WikipediaImportResult>(ok.Value);
        Assert.Equal("Aye-aye", result.CommonName);
        Assert.Equal<decimal?>(2500m, result.WeightGramsMin);
        Assert.DoesNotContain(handler.RequestedKeys, k => k.StartsWith("search/"));
    }

    [Fact]
    public async Task Unknown_name_returns_not_found()
    {
        var (controller, _) = Create();

        var response = await controller.Import("qwxzvbnmplkjhgf animal");

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://en.wikipedia.org/w/index.php?title=Okapi")]
    public async Task Empty_query_or_non_article_link_returns_bad_request(string query)
    {
        var (controller, handler) = Create();

        var response = await controller.Import(query);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Empty(handler.RequestedKeys);
    }

    [Fact]
    public async Task Too_long_query_returns_bad_request()
    {
        var (controller, handler) = Create();

        var response = await controller.Import(new string('a', 301));

        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Empty(handler.RequestedKeys);
    }

    [Fact]
    public async Task Unreachable_wikipedia_returns_bad_gateway()
    {
        var (controller, handler) = Create();
        handler.Failure = new HttpRequestException("offline");

        var response = await controller.Import("okapi");

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public async Task Slow_wikipedia_returns_gateway_timeout()
    {
        var (controller, handler) = Create();
        handler.Failure = new TaskCanceledException("timeout");

        var response = await controller.Import("okapi");

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(504, result.StatusCode);
    }

    [Fact]
    public async Task Okapi_keeps_wikipedia_lifespan_over_anage()
    {
        var (controller, handler) = Create();
        
        var response = await controller.Import("okapi");

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<WikipediaImportResult>(ok.Value);
        Assert.Equal(360m, result.LifespanMonthsMax);
    }

    [Fact]
    public async Task Import_still_works_when_eol_is_down()
    {
        var (controller, handler) = Create();
        handler.FailNext("eol-search/Okapia_johnstoni", HttpStatusCode.InternalServerError);
        var response = await controller.Import("okapi");
        
        Assert.IsType<OkObjectResult>(response.Result);
    }
    
    [Fact]
    public async Task Shoebill_gets_lifespan_from_eol_when_anage_has_none()
    {
        var realAnAge = Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "anage_data.txt");
        var emptyAnAge = Path.GetTempFileName();
        File.WriteAllLines(emptyAnAge, File.ReadLines(realAnAge).Take(1));

        var (importer, handler) = TestImporter.Create();
        handler.Override("eol-search/Balaeniceps_rex", """{"results":[{"id":3}]}""");
        handler.Override("eol-page/3", """{"taxonConcept":{"dataObjects":[{"description":"<strong>Range lifespan<br><span>Status: captivity</span>:</strong> 22 to 31 years.<strong>Range lifespan<br><span>Status: wild</span>:</strong> 8 (high) years."}]}}""");

        var anAge = new AnAgeSource(emptyAnAge);
        var sealife = new SeaLifeBaseSource( 
            Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "sealifebase_data.txt"), 
            Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "fishbase_data.txt"));
        var eol = new EolClient(new HttpClient(handler));
        var controller = new WikipediaController(importer, anAge, sealife, eol);

        var response = await controller.Import("shoebill");

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<WikipediaImportResult>(ok.Value);
        Assert.Equal(96m, result.LifespanMonthsMax);
    }
}
