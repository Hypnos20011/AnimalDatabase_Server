using System.Net;
using System.Net.Http.Json;
using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;

namespace AnimalDatabase.Api.Tests;

public class AnimalTranslationsEndpointTests
{
    private static HttpRequestMessage Put(string language, AnimalTexts texts, string? adminKey = ApiFactory.TestKey, int id = 1)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/animals/{id}/translations/{language}")
        {
            Content = JsonContent.Create(texts)
        };
        if (adminKey is not null)
        {
            request.Headers.Add("X-Admin-Key", adminKey);
        }
        return request;
    }

    private static AnimalTexts German() => new()
    {
        CommonName = "Okapi",
        Habitat = "Regenwald",
        RangeRegion = "Zentralafrika",
        Profile = "Eine Waldgiraffe. Fun fact: Ihre Zunge ist sehr lang."
    };

    [Fact]
    public async Task Request_without_admin_key_returns_unauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Put("de", German(), adminKey: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_with_wrong_admin_key_returns_unauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Put("de", German(), adminKey: "wrong-key"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("it")]
    [InlineData("xx")]
    [InlineData("en")]
    [InlineData("EN")]
    public async Task Unsupported_or_source_language_returns_bad_request(string language)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Put(language, German()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid language", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Common_name_over_120_characters_returns_bad_request()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var texts = German();
        texts.CommonName = new string('a', 121);

        using var response = await client.SendAsync(Put("de", texts));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CommonName", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Habitat")]
    [InlineData("RangeRegion")]
    public async Task Habitat_or_range_over_255_characters_returns_bad_request(string field)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var texts = German();
        if (field == "Habitat")
        {
            texts.Habitat = new string('a', 256);
        }
        else
        {
            texts.RangeRegion = new string('a', 256);
        }

        using var response = await client.SendAsync(Put("de", texts));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Translate_endpoint_also_rejects_a_common_name_over_120_characters()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/translate")
        {
            Content = JsonContent.Create(new AnimalTexts { CommonName = new string('a', 121) })
        };
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CommonName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Non_numeric_id_does_not_match_the_route()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/animals/abc/translations/de")
        {
            Content = JsonContent.Create(German())
        };
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);

        using var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static HttpRequestMessage Get(string language, int id = 1)
    {
        return new HttpRequestMessage(HttpMethod.Get, $"/api/animals/{id}/translations/{language}");
    }

    [Theory]
    [InlineData("it")]
    [InlineData("xx")]
    [InlineData("en")]
    [InlineData("EN")]
    public async Task Get_with_unsupported_or_source_language_returns_bad_request(string language)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Get(language));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid language", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Get_translation_does_not_require_the_admin_key()
    {
        var action = typeof(AnimalsController).GetMethod(nameof(AnimalsController.GetTranslation));
        Assert.NotNull(action);
        Assert.Null(action!.GetCustomAttributes(typeof(RequireRoleAttribute), inherit: true).FirstOrDefault());
        Assert.Null(typeof(AnimalsController).GetCustomAttributes(typeof(RequireRoleAttribute), inherit: true).FirstOrDefault());
    }
}
