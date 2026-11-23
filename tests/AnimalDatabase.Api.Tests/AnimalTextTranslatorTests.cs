using System.Net;
using System.Text;
using System.Text.Json;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class AnimalTextTranslatorTests
{
    private sealed class EchoDeeplHandler : HttpMessageHandler
    {
        public List<string[]> Requests { get; } = new();
        public List<string?> Targets { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            var texts = json.RootElement.GetProperty("text").EnumerateArray().Select(t => t.GetString()!).ToArray();
            Requests.Add(texts);
            Targets.Add(json.RootElement.GetProperty("target_lang").GetString());
            var response = JsonSerializer.Serialize(new { translations = texts.Select(t => new { text = "DE:" + t }) });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }

    private static (AnimalTextTranslator Translator, EchoDeeplHandler Handler) Create()
    {
        var handler = new EchoDeeplHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-free.deepl.com/") };
        return (new AnimalTextTranslator(new DeeplTranslator(http)), handler);
    }

    [Fact]
    public async Task Translates_all_four_fields_into_the_right_places()
    {
        var (translator, _) = Create();

        var german = await translator.TranslateAsync(new AnimalTexts
        {
            CommonName = "Axolotl",
            Habitat = "Freshwater lakes",
            RangeRegion = "Mexico",
            Profile = "A salamander that stays young."
        });

        Assert.Equal("DE:Axolotl", german.CommonName);
        Assert.Equal("DE:Freshwater lakes", german.Habitat);
        Assert.Equal("DE:Mexico", german.RangeRegion);
        Assert.Equal("DE:A salamander that stays young.", german.Profile);
    }

    [Fact]
    public async Task Sends_everything_in_one_request()
    {
        var (translator, handler) = Create();

        await translator.TranslateAsync(new AnimalTexts
        {
            CommonName = "Axolotl",
            Habitat = "Lakes",
            RangeRegion = "Mexico",
            Profile = "Stays young. Fun fact: Regrows limbs."
        });

        Assert.Single(handler.Requests);
        Assert.Equal(["Axolotl", "Lakes", "Mexico", "Stays young.", "Regrows limbs."], handler.Requests[0]);
    }

    [Fact]
    public async Task Translates_description_and_fun_fact_separately_and_keeps_the_marker()
    {
        var (translator, _) = Create();

        var german = await translator.TranslateAsync(new AnimalTexts
        {
            Profile = "Stays young. Fun fact: Regrows limbs."
        });

        Assert.Equal("DE:Stays young. Fun fact: DE:Regrows limbs.", german.Profile);
    }

    [Fact]
    public async Task French_target_keeps_the_exact_fun_fact_marker()
    {
        var (translator, handler) = Create();

        var french = await translator.TranslateAsync(new AnimalTexts
        {
            Profile = "Stays young. Fun fact: Regrows limbs."
        }, "fr");

        Assert.Equal(["Stays young.", "Regrows limbs."], handler.Requests[0]);
        Assert.Equal("FR", handler.Targets[0]);
        Assert.Equal("DE:Stays young. Fun fact: DE:Regrows limbs.", french.Profile);
    }

    [Fact]
    public async Task Keeps_the_marker_exactly_as_written()
    {
        var (translator, handler) = Create();

        var german = await translator.TranslateAsync(new AnimalTexts
        {
            Profile = "Looks sad.\n\nFun Fact: Voted ugliest animal."
        });

        Assert.Equal(["Looks sad.", "Voted ugliest animal."], handler.Requests[0]);
        Assert.Equal("DE:Looks sad.\n\nFun Fact: DE:Voted ugliest animal.", german.Profile);
    }

    [Fact]
    public async Task Only_splits_at_the_first_marker()
    {
        var (translator, handler) = Create();

        await translator.TranslateAsync(new AnimalTexts
        {
            Profile = "Odd. Fun fact: One. Fun fact: Two."
        });

        Assert.Equal(["Odd.", "One. Fun fact: Two."], handler.Requests[0]);
    }

    [Fact]
    public async Task Skips_empty_fields_and_keeps_the_others_in_place()
    {
        var (translator, handler) = Create();

        var german = await translator.TranslateAsync(new AnimalTexts
        {
            CommonName = "Blobfish",
            Habitat = null,
            RangeRegion = "   ",
            Profile = "Lives deep down."
        });

        Assert.Equal(["Blobfish", "Lives deep down."], handler.Requests[0]);
        Assert.Equal("DE:Blobfish", german.CommonName);
        Assert.Null(german.Habitat);
        Assert.Null(german.RangeRegion);
        Assert.Equal("DE:Lives deep down.", german.Profile);
    }

    [Fact]
    public async Task Does_not_call_deepl_when_everything_is_empty()
    {
        var (translator, handler) = Create();

        var german = await translator.TranslateAsync(new AnimalTexts { CommonName = "", Profile = " " });

        Assert.Empty(handler.Requests);
        Assert.Null(german.CommonName);
        Assert.Null(german.Habitat);
        Assert.Null(german.RangeRegion);
        Assert.Null(german.Profile);
    }

    [Fact]
    public async Task Passes_deepl_errors_on()
    {
        var handler = new FailingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-free.deepl.com/") };
        var translator = new AnimalTextTranslator(new DeeplTranslator(http));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            translator.TranslateAsync(new AnimalTexts { CommonName = "Axolotl" }));

        Assert.Equal((HttpStatusCode)456, error.StatusCode);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)456));
    }
}