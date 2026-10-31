using System.Globalization;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class AnAgeSource
{
    private readonly Dictionary<string, TraitData> _animals = new();

    public AnAgeSource(string filePath)
    {
        foreach (var line in File.ReadLines(filePath).Skip(1))
        {
            var columns = line.Split('\t');
            if (columns.Length < 21) continue;
            var key = Key(columns[6], columns[7]);
            var weightGrams = ParseNumber(columns[18]);
            var longevity = ParseNumber(columns[20]);

            _animals[key] = new TraitData
            {
                WeightKgMax = weightGrams / 1000,
                LifespanYearsMax = longevity is null ? null : (int)Math.Round(longevity.Value)
            };
        }
    }

    public TraitData? Find(string scientificName)
    {
        var parts = scientificName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        return _animals.GetValueOrDefault(Key(parts[0], parts[1]));
    }

    private static string Key(string genus, string species)
    {
        return $"{genus} {species}".ToLowerInvariant();
    }

    private static decimal? ParseNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}