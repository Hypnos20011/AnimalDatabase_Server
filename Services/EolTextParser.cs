using System.Globalization;
using System.Text.RegularExpressions;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class EolTextParser
{
    private static readonly Dictionary<string, decimal> MassToKg = new()
    {
        ["g"] = 0.001m,
        ["kg"] = 1,
    };

    private static readonly Dictionary<string, decimal> LengthToCm = new()
    {
        ["mm"] = 0.1m,
        ["cm"] = 1,
        ["m"] = 100,
    };

    public static TraitData Parse(string text)
    {
        var (weightMin, weightMax) = ReadRange(text, "Range mass", MassToKg);
        var (lengthMin, lengthMax) = ReadRange(text, "Range length", LengthToCm);

        return new TraitData
        {
            WeightKgMin = weightMin,
            WeightKgMax = weightMax,
            SizeLengthCmMin = lengthMin,
            SizeLengthCmMax = lengthMax,
        };
    }

    private static (decimal? Min, decimal? Max) ReadRange(string text, string label, Dictionary<string, decimal> units)
    {
        var match = Regex.Match(text,
            label + @":</strong>\s*([\d.,]+)(?:\s*to\s*([\d.,]+))?\s*(?:\((high|low)\))?\s*([a-z]+)");
        if (!match.Success) return (null, null);
        if (!units.TryGetValue(match.Groups[4].Value, out var factor)) return (null, null);

        var first = ParseNumber(match.Groups[1].Value) * factor;
        if (match.Groups[2].Success) return (first, ParseNumber(match.Groups[2].Value) * factor);
        if (match.Groups[3].Value == "low") return (first, null);
        if (match.Groups[3].Value == "high") return (null, first);
        return (first, first);
    }

    private static decimal? ParseNumber(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
}