using System.Text.Json.Serialization;

namespace AnimalDatabase.Api.Services;

public class LibreTranslateTranslator : IBatchTranslator
{
    private readonly string? _apiKey;
    private readonly HttpClient _http;

    public LibreTranslateTranslator(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["LibreTranslate:ApiKey"];
    }

    public async Task<string[]> TranslateAsync(string[] texts)
    {
        var request = new LibreRequest(texts, "en", "de", "text", _apiKey);
        using var response = await _http.PostAsJsonAsync("translate", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LibreResponse>();
        var translated = body!.TranslatedText;
        return translated is string one
            ? new[] { one }
            : ((System.Text.Json.JsonElement)translated).EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();
    }

    private record LibreRequest(
        [property: JsonPropertyName("q")] string[] Q,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("api_key")]
        string? ApiKey);

    private record LibreResponse(
        [property: JsonPropertyName("translatedText")]
        object TranslatedText);
}