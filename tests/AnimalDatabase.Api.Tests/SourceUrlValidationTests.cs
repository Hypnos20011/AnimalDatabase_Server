using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalDatabase.Api.Models;
using Xunit;

namespace AnimalDatabase.Api.Tests;

public class SourceUrlValidationTests
{
    private static List<ValidationResult> Validate(string? sourceUrl)
    {
        var request = new CreateAnimalRequest { CommonName = "Okapi", SourceUrl = sourceUrl };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://en.wikipedia.org/wiki/Okapi")]
    public void Https_or_missing_source_is_accepted(string? sourceUrl)
    {
        Assert.Empty(Validate(sourceUrl));
    }

    [Theory]
    [InlineData("http://en.wikipedia.org/wiki/Okapi")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/okapi")]
    [InlineData("wikipedia")]
    public void Anything_but_https_is_rejected(string sourceUrl)
    {
        var results = Validate(sourceUrl);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(nameof(CreateAnimalRequest.SourceUrl), r.MemberNames));
    }

    [Fact]
    public void Too_long_source_is_rejected()
    {
        Assert.NotEmpty(Validate("https://example.com/" + new string('a', 500)));
    }

    [Fact]
    public void Source_url_survives_a_json_round_trip()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(new CreateAnimalRequest { CommonName = "Okapi", SourceUrl = "https://en.wikipedia.org/wiki/Okapi" }, options);
        Assert.Contains("\"sourceUrl\":\"https://en.wikipedia.org/wiki/Okapi\"", json);
        var back = JsonSerializer.Deserialize<CreateAnimalRequest>(json, options)!;
        Assert.Equal("https://en.wikipedia.org/wiki/Okapi", back.SourceUrl);
    }

    private static HttpRequestMessage Send(HttpMethod method, string url, string sourceUrl)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(new { commonName = "Okapi", sourceUrl })
        };
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);
        return request;
    }

    [Theory]
    [InlineData("POST", "http://en.wikipedia.org/wiki/Okapi")]
    [InlineData("POST", "javascript:alert(1)")]
    [InlineData("PUT", "http://en.wikipedia.org/wiki/Okapi")]
    [InlineData("PUT", "javascript:alert(1)")]
    public async Task Api_rejects_non_https_source_on_create_and_update(string method, string sourceUrl)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var url = method == "POST" ? "/api/animals" : "/api/animals/1";

        using var response = await client.SendAsync(Send(new HttpMethod(method), url, sourceUrl));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("SourceUrl", await response.Content.ReadAsStringAsync());
    }
}