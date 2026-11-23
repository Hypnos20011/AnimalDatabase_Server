using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TranslationGaps
{
    public const int MaxCharacters = 3000;

    public static readonly IReadOnlyList<string> Languages = ["de", "fr"];

    private static readonly string[] FieldNames = ["commonName", "habitat", "rangeRegion", "profile"];

    public static IReadOnlyList<string> Missing(AnimalTexts english, AnimalTexts? translation)
    {
        var missing = new List<string>();
        foreach (var field in FieldNames)
        {
            if (!string.IsNullOrWhiteSpace(Read(english, field)) &&
                string.IsNullOrWhiteSpace(translation is null ? null : Read(translation, field)))
            {
                missing.Add(field);
            }
        }

        return missing;
    }

    public static IReadOnlyList<MissingTranslationReport> Reports(IEnumerable<EnglishAnimalRecord> animals)
    {
        var reports = new List<MissingTranslationReport>();
        foreach (var animal in animals)
        {
            var languages = new List<MissingTranslationLanguage>();
            foreach (var language in Languages)
            {
                var missing = Missing(animal.English.ToTexts(), Stored(animal, language));
                if (missing.Count == 0)
                {
                    continue;
                }

                languages.Add(new MissingTranslationLanguage
                {
                    Language = language,
                    Fields = missing
                });
            }

            if (languages.Count == 0)
            {
                continue;
            }

            reports.Add(new MissingTranslationReport
            {
                Id = animal.English.Id,
                CommonName = animal.English.CommonName ?? "",
                Languages = languages
            });
        }

        return reports;
    }

    public static AnimalTexts Pick(AnimalTexts english, IReadOnlyCollection<string> fields)
    {
        var picked = new AnimalTexts();
        foreach (var field in fields)
        {
            Write(picked, field, Read(english, field));
        }

        return picked;
    }

    public static AnimalTexts Overlay(AnimalTexts? existing, AnimalTexts translated, IReadOnlyCollection<string> fields)
    {
        var selected = new HashSet<string>(fields, StringComparer.Ordinal);
        var result = new AnimalTexts
        {
            CommonName = existing?.CommonName,
            Habitat = existing?.Habitat,
            RangeRegion = existing?.RangeRegion,
            Profile = existing?.Profile
        };
        foreach (var field in selected)
        {
            Write(result, field, Read(translated, field));
        }

        return result;
    }

    public static bool TooLong(AnimalTexts texts)
    {
        var length = (texts.CommonName?.Length ?? 0) +
                     (texts.Habitat?.Length ?? 0) +
                     (texts.RangeRegion?.Length ?? 0) +
                     (texts.Profile?.Length ?? 0);
        return length > MaxCharacters;
    }

    private static AnimalTexts? Stored(EnglishAnimalRecord animal, string language) => language switch
    {
        "de" => animal.German,
        "fr" => animal.French,
        _ => null
    };

    private static string? Read(AnimalTexts texts, string field) => field switch
    {
        "commonName" => texts.CommonName,
        "habitat" => texts.Habitat,
        "rangeRegion" => texts.RangeRegion,
        "profile" => texts.Profile,
        _ => null
    };

    private static void Write(AnimalTexts texts, string field, string? value)
    {
        switch (field)
        {
            case "commonName":
                texts.CommonName = value;
                break;
            case "habitat":
                texts.Habitat = value;
                break;
            case "rangeRegion":
                texts.RangeRegion = value;
                break;
            case "profile":
                texts.Profile = value;
                break;
        }
    }
}
