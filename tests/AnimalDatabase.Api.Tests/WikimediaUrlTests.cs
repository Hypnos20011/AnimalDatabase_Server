using AnimalDatabase.Api.Services;
using Xunit;

namespace AnimalDatabase.Api.Tests;

public class WikimediaUrlTests
{
    private const string Gharial1280 =
        "https://thumb.wikimedia.org/wikipedia/commons/thumb/9/90/Gharial_%28Gavialis_gangeticus%29_female.jpg/1280px-Gharial_%28Gavialis_gangeticus%29_female.jpg";

    [Fact]
    public void Small_thumbnail_with_tracking_becomes_1280_without_query()
    {
        var pasted =
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/9/90/Gharial_%28Gavialis_gangeticus%29_female.jpg/250px-Gharial_%28Gavialis_gangeticus%29_female.jpg?utm_source=en.wikipedia.org&utm_campaign=parser&utm_content=thumbnail";

        Assert.Equal(Gharial1280, WikimediaUrl.Normalize(pasted));
    }

    [Fact]
    public void A_1280_thumbnail_stays_the_same()
    {
        Assert.Equal(Gharial1280, WikimediaUrl.Normalize(Gharial1280));
    }

    [Fact]
    public void Upload_host_thumbnails_are_normalized_too()
    {
        var url = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3d/Okapi.jpg/500px-Okapi.jpg";

        Assert.Equal(
            "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3d/Okapi.jpg/1280px-Okapi.jpg",
            WikimediaUrl.Normalize(url));
    }

    [Theory]
    [InlineData("https://upload.wikimedia.org/wikipedia/commons/3/3d/Okapi.jpg")]
    [InlineData("https://example.com/thumb/3/3d/Okapi.jpg/250px-Okapi.jpg")]
    [InlineData("https://example.com/okapi.jpg?size=250")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Other_urls_stay_unchanged(string url)
    {
        Assert.Equal(url, WikimediaUrl.Normalize(url));
    }
}