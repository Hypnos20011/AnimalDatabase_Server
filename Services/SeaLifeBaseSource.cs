using System.Globalization;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class SeaLifeBaseSource
{
    private static readonly HashSet<string> BodyLengthTypes = new() { "TL", "SL", "FL" };
    private readonly Dictionary<string, TraitData> _animals = new();

    public SeaLifeBaseSource(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        foreach (var line in File.ReadAllLines(filePath).Skip(1))
        {
            var columns = line.Split('\t');
            if (columns.Length < 6) continue;

            var length = ParseNumber(columns[2]);
            var lengthType = columns[3];
            var weightGrams = ParseNumber(columns[4]);
            var longevity = ParseNumber(columns[5]);

            if (!BodyLengthTypes.Contains(lengthType)) length = null;
            if (length <= 0) length = null;
            if (weightGrams <= 0) weightGrams = null;
            if (weightGrams > length * length * length) weightGrams = null;

            _animals[Key(columns[0], columns[1])] = new TraitData
            {
                SizeLengthCmMax = length,
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