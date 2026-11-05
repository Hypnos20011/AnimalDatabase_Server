using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class ImageAttributionMappingTests
{
    [Fact]
    public void Maps_artist_and_descriptionurl_onto_import_result()
    {
        var result = new WikipediaImportResult
        {
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9f/Okapia_johnstoni_17zz.jpg/1280px-Okapia_johnstoni_17zz.jpg"
        };
        var infos = new Dictionary<string, WikipediaImporter.ImageInfo>
        {
            ["Okapia johnstoni 17zz.jpg"] = new(
                "Okapia johnstoni 17zz.jpg",
                "image/jpeg",
                1000,
                1348,
                "https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg",
                null,
                null,
                null,
                null,
                "  Photo by   David J. Stang  ",
                "2007-02-17",
                "https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg?utm_source=x")
        };

        WikipediaImporter.FillImageAttribution(result, infos);

        Assert.Equal("Photo by David J. Stang", result.Attribution);
        Assert.Equal("https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg", result.SourcePageUrl);
    }

    [Fact]
    public void Skips_non_https_descriptionurl_and_blank_artist()
    {
        var result = new WikipediaImportResult
        {
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/1/12/Okapi.jpg"
        };
        var infos = new Dictionary<string, WikipediaImporter.ImageInfo>
        {
            ["Okapi.jpg"] = new(
                "Okapi.jpg",
                "image/jpeg",
                2000,
                1500,
                null,
                null,
                null,
                null,
                null,
                "   ",
                null,
                "http://commons.wikimedia.org/wiki/File:Okapi.jpg")
        };

        WikipediaImporter.FillImageAttribution(result, infos);

        Assert.Null(result.Attribution);
        Assert.Null(result.SourcePageUrl);
    }

    [Fact]
    public void Leaves_fields_unset_when_image_metadata_is_missing()
    {
        var result = new WikipediaImportResult
        {
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/1/12/Missing.jpg"
        };

        WikipediaImporter.FillImageAttribution(result, new Dictionary<string, WikipediaImporter.ImageInfo>());

        Assert.Null(result.Attribution);
        Assert.Null(result.SourcePageUrl);
    }

    [Fact]
    public async Task Import_fills_attribution_from_stub_imageinfo_artist()
    {
        var (importer, handler) = TestImporter.Create();
        handler.Override(
            "pageimages/Okapi",
            """
            {"query":{"pages":[{"title":"Okapi","thumbnail":{"source":"https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg","width":1000,"height":1348}}]}}
            """);
        handler.Override(
            "imageinfo/Okapi",
            """
            {"query":{"pages":[{"title":"File:Okapia johnstoni 17zz.jpg","imageinfo":[{"mime":"image/jpeg","width":1000,"height":1348,"url":"https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg","thumburl":"https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg","descriptionurl":"https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg","extmetadata":{"Artist":{"value":"<b>Charles J. Sharp</b>"},"ObjectName":{"value":"Okapi"},"ImageDescription":{"value":"Okapi at a zoo"},"Categories":{"value":"Okapia johnstoni"},"DateTimeOriginal":{"value":"2012-01-01"}}}]}]}}
            """);

        var result = await importer.ImportByNameAsync("okapi");

        Assert.NotNull(result);
        Assert.Equal("https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg", result.ImageUrl);
        Assert.Equal("Charles J. Sharp", result.Attribution);
        Assert.Equal("https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg", result.SourcePageUrl);
    }
}
