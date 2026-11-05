using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalDatabase.Api.Models;
using Xunit;

namespace AnimalDatabase.Api.Tests;

public class ImageAttributionValidationTests
{
    private static List<ValidationResult> ValidateImage(string? sourcePageUrl, string? attribution = null)
    {
        var request = new CreateAnimalImageRequest
        {
            Kind = "unspecified",
            Url = "https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg",
            Attribution = attribution,
            SourcePageUrl = sourcePageUrl
        };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg")]
    public void Https_or_missing_source_page_is_accepted(string? sourcePageUrl)
    {
        Assert.Empty(ValidateImage(sourcePageUrl, "Charles J. Sharp"));
    }

    [Theory]
    [InlineData("http://commons.wikimedia.org/wiki/File:Okapi.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/okapi")]
    [InlineData("commons")]
    public void Anything_but_https_source_page_is_rejected(string sourcePageUrl)
    {
        var results = ValidateImage(sourcePageUrl);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(nameof(CreateAnimalImageRequest.SourcePageUrl), r.MemberNames));
    }

    [Fact]
    public void Too_long_attribution_is_rejected()
    {
        Assert.NotEmpty(ValidateImage(null, new string('a', 501)));
    }

    [Fact]
    public void Attribution_fields_survive_a_json_round_trip()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var image = new CreateAnimalImageRequest
        {
            Kind = "unspecified",
            Url = "https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg",
            Attribution = "Charles J. Sharp / CC BY-SA 4.0",
            SourcePageUrl = "https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg"
        };
        var json = JsonSerializer.Serialize(image, options);
        Assert.Contains("\"attribution\":\"Charles J. Sharp / CC BY-SA 4.0\"", json);
        Assert.Contains("\"sourcePageUrl\":\"https://commons.wikimedia.org/wiki/File:Okapia_johnstoni_17zz.jpg\"", json);
        var back = JsonSerializer.Deserialize<CreateAnimalImageRequest>(json, options)!;
        Assert.Equal(image.Attribution, back.Attribution);
        Assert.Equal(image.SourcePageUrl, back.SourcePageUrl);
    }

    private static HttpRequestMessage Send(HttpMethod method, string url)
    {
        var body = new
        {
            commonName = "Okapi",
            images = new[]
            {
                new
                {
                    kind = "unspecified",
                    url = "https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg",
                    sourcePageUrl = "http://commons.wikimedia.org/wiki/File:Okapi.jpg"
                }
            }
        };
        var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);
        return request;
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Api_rejects_non_https_image_source_page_on_create_and_update(string method)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var url = method == "POST" ? "/api/animals" : "/api/animals/1";

        using var response = await client.SendAsync(Send(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("SourcePageUrl", await response.Content.ReadAsStringAsync());
    }
}