using System.Net;
using System.Text;
using System.Text.Json;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class DeeplTranslatorTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public CapturingHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }

    private static (DeeplTranslator Translator, CapturingHandler Handler) Create(
        HttpStatusCode status = HttpStatusCode.OK,
        string body = """{"translations":[]}""")
    {
        var handler = new CapturingHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-free.deepl.com/") };
        return (new DeeplTranslator(http), handler);
    }

    [Fact]
    public async Task Posts_to_the_translate_endpoint()
    {
        var (translator, handler) = Create(body: """{"translations":[{"text":"Hallo"}]}""");

        await translator.TranslateAsync(["Hello"]);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api-free.deepl.com/v2/translate", handler.Uri?.ToString());
    }

    [Fact]
    public async Task Sends_texts_and_languages_with_deepl_json_names()
    {
        var (translator, handler) = Create(body: """{"translations":[{"text":"Tier"},{"text":"Meer"}]}""");

        await translator.TranslateAsync(["Animal", "Sea"]);

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal(["Animal", "Sea"], root.GetProperty("text").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal("EN", root.GetProperty("source_lang").GetString());
        Assert.Equal("DE", root.GetProperty("target_lang").GetString());
        Assert.False(root.TryGetProperty("sourceLang", out _));
        Assert.False(root.TryGetProperty("targetLang", out _));
    }

    [Fact]
    public async Task Returns_translations_in_the_same_order()
    {
        var (translator, _) = Create(body:
            """{"translations":[{"detected_source_language":"EN","text":"Axolotl"},{"detected_source_language":"EN","text":"Seen in Mexiko"},{"detected_source_language":"EN","text":"Mexiko-Stadt"}]}""");

        var result = await translator.TranslateAsync(["Axolotl", "Lakes in Mexico", "Mexico City"]);

        Assert.Equal(["Axolotl", "Seen in Mexiko", "Mexiko-Stadt"], result);
    }

    [Fact]
    public async Task Keeps_german_umlauts_from_the_response()
    {
        var (translator, _) = Create(body: """{"translations":[{"text":"Grönland-Hai lebt in kühlen Gewässern"}]}""");

        var result = await translator.TranslateAsync(["Greenland shark lives in cold waters"]);

        Assert.Equal("Grönland-Hai lebt in kühlen Gewässern", result[0]);
    }

    [Fact]
    public async Task Throws_when_the_quota_is_used_up()
    {
        var (translator, _) = Create((HttpStatusCode)456, """{"message":"Quota exceeded"}""");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal((HttpStatusCode)456, error.StatusCode);
    }

    [Fact]
    public async Task Throws_when_the_key_is_rejected()
    {
        var (translator, _) = Create(HttpStatusCode.Forbidden, """{"message":"Wrong key"}""");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
    }
}