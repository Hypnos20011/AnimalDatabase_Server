using System.Text.RegularExpressions;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class AnimalTextTranslator
{
    private static readonly Regex FunFactMarker = new(@"\s*Fun fact:\s*", RegexOptions.IgnoreCase);
    private readonly DeeplTranslator _deepl;

    public AnimalTextTranslator(DeeplTranslator deepl)
    {
        _deepl = deepl;
    }

    public async Task<AnimalTexts> TranslateAsync(AnimalTexts english)
    {
        var marker = english.Profile is null ? Match.Empty : FunFactMarker.Match(english.Profile);
        var description = marker.Success ? english.Profile![..marker.Index] : english.Profile;
        var funFact = marker.Success ? english.Profile![(marker.Index + marker.Length)..] : null;
        string?[] pieces = [english.CommonName, english.Habitat, english.RangeRegion, description, funFact];
        var toSend = pieces.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!).ToArray();
        var translated = toSend.Length == 0 ? [] : await _deepl.TranslateAsync(toSend);
        var next = 0;
        var german = pieces.Select(p => string.IsNullOrWhiteSpace(p) ? null : translated[next++]).ToArray();

        return new AnimalTexts
        {
            CommonName = german[0],
            Habitat = german[1],
            RangeRegion = german[2],
            Profile = german[4] is null ? german[3] : german[3] + marker.Value + german[4]
        };
    }
}