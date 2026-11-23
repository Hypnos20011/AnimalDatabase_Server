using System.Net;
using System.Text.Json;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalDatabase.Api.Tests;

public class MissingTranslationEndpointTests
{
    [Fact]
    public async Task Missing_translations_is_unauthorized_without_the_key_and_ok_with_it()
    {
        var store = new StubTranslationStore();
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAnimalTranslationStore>(store)));
        var client = factory.CreateClient();

        using var denied = await client.GetAsync("/api/admin/missing-translations");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/missing-translations");
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);
        using var allowed = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using var json = JsonDocument.Parse(await allowed.Content.ReadAsStringAsync());
        var animal = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(4, animal.GetProperty("id").GetInt32());
        Assert.Equal("Okapi", animal.GetProperty("commonName").GetString());
        var languages = animal.GetProperty("languages").EnumerateArray().ToList();
        Assert.Equal("de", languages[0].GetProperty("language").GetString());
        Assert.Equal(
            ["habitat", "profile"],
            languages[0].GetProperty("fields").EnumerateArray().Select(field => field.GetString()!).ToArray());
        Assert.Equal("fr", languages[1].GetProperty("language").GetString());
        Assert.Equal(
            ["commonName", "habitat", "profile"],
            languages[1].GetProperty("fields").EnumerateArray().Select(field => field.GetString()!).ToArray());
    }

    [Fact]
    public async Task Translate_missing_without_the_key_is_unauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/animals/1/translate-missing", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Translate_missing_returns_not_found_for_an_unknown_animal()
    {
        var store = new StubTranslationStore();
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAnimalTranslationStore>(store)));
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/animals/9/translate-missing");
        request.Headers.Add("X-Admin-Key", ApiFactory.TestKey);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class StubTranslationStore : IAnimalTranslationStore
    {
        public Task<AnimalTexts?> GetTranslationAsync(int id, string language) =>
            Task.FromResult<AnimalTexts?>(null);

        public Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts) =>
            Task.FromResult(true);

        public Task<EnglishAnimal?> GetEnglishAsync(int id) =>
            Task.FromResult<EnglishAnimal?>(null);

        public Task<IReadOnlyList<EnglishAnimalRecord>> ListEnglishAsync() =>
            Task.FromResult<IReadOnlyList<EnglishAnimalRecord>>(
            [
                new EnglishAnimalRecord
                {
                    English = new EnglishAnimal
                    {
                        Id = 4,
                        CommonName = "Okapi",
                        Habitat = "Rainforest",
                        Profile = "A forest giraffe. Fun fact: Its tongue is very long."
                    },
                    German = new AnimalTexts { CommonName = "Okapi" }
                }
            ]);
    }
}
