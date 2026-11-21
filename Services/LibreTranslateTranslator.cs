using System.Text.Json;
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

    public async Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de")
    {
        var target = string.Equals(targetLanguage, "fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "de";
        var request = new LibreRequest(texts, "en", target, "text", _apiKey);
        using var response = await _http.PostAsJsonAsync("translate", request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException("LibreTranslate returned invalid JSON.", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("translatedText", out var translated) ||
                translated.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new HttpRequestException("LibreTranslate response did not include translatedText.");
            }

            if (translated.ValueKind == JsonValueKind.String)
            {
                return [translated.GetString()!];
            }

            if (translated.ValueKind != JsonValueKind.Array)
            {
                throw new HttpRequestException("LibreTranslate returned an unexpected translation shape.");
            }

            var values = new string[translated.GetArrayLength()];
            var index = 0;
            foreach (var item in translated.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    throw new HttpRequestException("LibreTranslate returned an unexpected translation shape.");
                }

                values[index++] = item.GetString()!;
            }

            return values;
        }
    }

    private record LibreRequest(
        [property: JsonPropertyName("q")] string[] Q,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("api_key")]
        string? ApiKey);
}
