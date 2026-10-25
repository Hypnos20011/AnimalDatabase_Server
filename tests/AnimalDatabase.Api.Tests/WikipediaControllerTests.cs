using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class WikipediaControllerTests
{
    private static (WikipediaController Controller, FixtureHttpHandler Handler) Create()
    {
        var (importer, handler) = TestImporter.Create();
        var anAge = new AnAgeSource(Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "anage_data.txt"));
        return (new WikipediaController(importer, anAge), handler);
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
        Assert.Equal<decimal?>(2.5m, result.WeightKgMin);
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
        Assert.Equal<int?>(30, result.LifespanYearsMax);
    }
}
