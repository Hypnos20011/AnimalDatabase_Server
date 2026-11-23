using System.Net;
using System.Text;
using System.Text.Json;
using AnimalDatabase.Api.Services;
using Microsoft.Extensions.Configuration;

namespace AnimalDatabase.Api.Tests;

public class LibreTranslateTranslatorTests
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

    private static (LibreTranslateTranslator Translator, CapturingHandler Handler) Create(
        HttpStatusCode status = HttpStatusCode.OK,
        string body = """{"translatedText":[]}""",
        string? apiKey = "test-key")
    {
        var handler = new CapturingHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LibreTranslate:ApiKey"] = apiKey
            })
            .Build();
        return (new LibreTranslateTranslator(http, config), handler);
    }

    [Fact]
    public async Task Posts_to_the_translate_endpoint()
    {
        var (translator, handler) = Create(body: """{"translatedText":["Hallo"]}""");

        await translator.TranslateAsync(["Hello"]);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("http://localhost:5000/translate", handler.Uri?.ToString());
    }

    [Fact]
    public async Task Sends_q_source_target_and_format_in_json()
    {
        var (translator, handler) = Create(body: """{"translatedText":["Tier","Meer"]}""");

        await translator.TranslateAsync(["Animal", "Sea"]);

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal(["Animal", "Sea"], root.GetProperty("q").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal("en", root.GetProperty("source").GetString());
        Assert.Equal("de", root.GetProperty("target").GetString());
        Assert.Equal("text", root.GetProperty("format").GetString());
        Assert.Equal("test-key", root.GetProperty("api_key").GetString());
    }

    [Fact]
    public async Task Sends_target_fr_for_FR()
    {
        var (translator, handler) = Create(body: """{"translatedText":["Bonjour"]}""");

        await translator.TranslateAsync(["Hello"], "FR");

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal("en", root.GetProperty("source").GetString());
        Assert.Equal("fr", root.GetProperty("target").GetString());
    }

    [Fact]
    public async Task Sends_target_de_by_default()
    {
        var (translator, handler) = Create(body: """{"translatedText":["Hallo"]}""");

        await translator.TranslateAsync(["Hello"]);

        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("de", json.RootElement.GetProperty("target").GetString());
    }

    [Fact]
    public async Task Error_object_without_translated_text_throws_http_request_exception()
    {
        var (translator, _) = Create(body: """{"error":"fr is not supported"}""");

        await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"], "fr"));
    }

    [Fact]
    public async Task Invalid_json_throws_http_request_exception()
    {
        var (translator, _) = Create(body: "this is not json");

        await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));
    }

    [Fact]
    public async Task Returns_translations_in_the_same_order()
    {
        var (translator, _) = Create(body: """{"translatedText":["Axolotl","Seen in Mexiko","Mexiko-Stadt"]}""");

        var result = await translator.TranslateAsync(["Axolotl", "Lakes in Mexico", "Mexico City"]);

        Assert.Equal(["Axolotl", "Seen in Mexiko", "Mexiko-Stadt"], result);
    }

    [Fact]
    public async Task Keeps_german_umlauts_from_the_response()
    {
        var (translator, _) = Create(body: """{"translatedText":["Grünland-Hai lebt in kühlen Gewässern"]}""");

        var result = await translator.TranslateAsync(["Greenland shark lives in cold waters"]);

        Assert.Equal("Grünland-Hai lebt in kühlen Gewässern", result[0]);
    }

    [Fact]
    public async Task Throws_when_the_server_returns_5xx()
    {
        var (translator, _) = Create(HttpStatusCode.InternalServerError, """{"error":"boom"}""");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
    }
}