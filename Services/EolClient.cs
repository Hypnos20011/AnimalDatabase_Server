using System.Text.Json;

namespace AnimalDatabase.Api.Services;

public class EolClient
{
    private readonly HttpClient _http;
    
    public EolClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> FindRangeTextAsync(string scientificName)
    {
        var searchUrl = $"https://eol.org/api/search/1.0.json?q={Uri.EscapeDataString(scientificName)}&exact=true";
     using var search = JsonDocument.Parse(await _http.GetStringAsync(searchUrl));
     var results = search.RootElement.GetProperty("results");
     if (results.GetArrayLength() == 0) return null;
     var id = results[0].GetProperty("id").GetInt32();

     var pageUrl = $"https://eol.org/api/pages/1.0/{id}.json?details=true&texts_per_page=75&images_per_page=0";
     using var page = JsonDocument.Parse(await _http.GetStringAsync(pageUrl));
     foreach (var item in page.RootElement.GetProperty("taxonConcept"). GetProperty("dataObjects").EnumerateArray())
     {
         if (!item.TryGetProperty("description", out var description)) continue;
         var text = description.GetString();
         if (text is not null && (text.Contains("Range mass") || text.Contains("Range length"))) return text;
     }

     return null;
    }
}