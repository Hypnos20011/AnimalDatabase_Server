using System.Web;

namespace AnimalDatabase.Api.Tests;

public class WikipediaImageTests
{
    [Fact]
    public async Task Asks_Wikipedia_for_a_1280px_page_image()
    {
        var (importer, handler) = TestImporter.Create();

        await importer.ImportByNameAsync("okapi");

        var pageImages = Assert.Single(handler.RequestedUris, uri => HttpUtility.ParseQueryString(uri.Query)["prop"] == "pageimages");
        Assert.Equal("1280", HttpUtility.ParseQueryString(pageImages.Query)["pithumbsize"]);
    }

    [Fact]
    public async Task Keeps_a_smaller_original_as_is_without_tracking_query()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override(
            "pageimages/Okapi",
            """
            {"query":{"pages":[{"title":"Okapi","thumbnail":{"source":"https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg?utm_source=en.wikipedia.org&utm_campaign=api&utm_content=thumbnail_unscaled","width":1000,"height":1348}}]}}
            """);

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task Missing_page_image_gives_no_image_url()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override("pageimages/Okapi", """{"query":{"pages":[{"title":"Okapi"}]}}""");

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Null(result.ImageUrl);
    }
}
