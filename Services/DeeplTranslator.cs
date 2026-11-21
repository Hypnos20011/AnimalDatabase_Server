using System.Text.Json.Serialization;

namespace AnimalDatabase.Api.Services;

public class DeeplTranslator : IBatchTranslator
{
    private readonly HttpClient _http;

    public DeeplTranslator(HttpClient http)
    {
        _http = http;
    }

    public async Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de")
    {
        var target = string.Equals(targetLanguage, "fr", StringComparison.OrdinalIgnoreCase) ? "FR" : "DE";
        var request = new DeeplRequest(texts, "EN", target);
        using var response = await _http.PostAsJsonAsync("v2/translate", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DeeplResponse>();
        return body!.Translations.Select(x => x.Text).ToArray();
    }

    private record DeeplRequest(
        string[] Text,
        [property: JsonPropertyName("source_lang")]
        string SourceLang,
        [property: JsonPropertyName("target_lang")]
        string TargetLang);

    private record DeeplResponse(DeeplTranslation[] Translations);

    private record DeeplTranslation(string Text);
}