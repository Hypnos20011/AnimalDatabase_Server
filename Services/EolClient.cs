using System.Text.Json;

namespace AnimalDatabase.Api.Services;

public class EolClient
{
    private const int MaxResults = 3;
    private const int MaxTextPages = 2;
    private readonly HttpClient _http;

    public EolClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> FindRangeTextAsync(string scientificName)
    {
        try
        {
            var searchUrl = $"https://eol.org/api/search/1.0.json?q={Uri.EscapeDataString(scientificName)}&exact=true";
            using var search = JsonDocument.Parse(await _http.GetStringAsync(searchUrl));

            foreach (var result in search.RootElement.GetProperty("results").EnumerateArray().Take(MaxResults))
            {
                var id = result.GetProperty("id").GetInt32();
                for (var textsPage = 1; textsPage <= MaxTextPages; textsPage++)
                {
                    var (text, empty) = await ReadPageAsync(id, textsPage);
                    if (text is not null) return text;
                    if (empty) break;
                }
            }

        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return null;
        }

        return null;
    }

    private async Task<(string? Text, bool Empty)> ReadPageAsync(int id, int textsPage)
    {
        var pageUrl = $"https://eol.org/api/pages/1.0/{id}.json?details=true&texts_per_page=75&texts_page={textsPage}&images_per_page=0";
        using var page = JsonDocument.Parse(await _http.GetStringAsync(pageUrl));
        if (!page.RootElement.GetProperty("taxonConcept").TryGetProperty("dataObjects", out var objects))
            return (null, true);

        foreach (var item in objects.EnumerateArray())
        {
            if (!item.TryGetProperty("description", out var description)) continue;
            var text = description.GetString();
            if (text is not null && (text.Contains("Range mass") || text.Contains("Range length"))) return (text, false);
        }

        return (null, objects.GetArrayLength() == 0);
    }
}