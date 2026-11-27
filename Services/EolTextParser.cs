using System.Globalization;
using System.Text.RegularExpressions;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class EolTextParser
{
    private const string ValuePattern =
        @"\s*([\d.,]+)(?:\s*to\s*([\d.,]+))?\s*(?:\((high|low)\))?\s*([a-z]+)";

    private static readonly Dictionary<string, decimal> MassToGrams = new()
    {
        ["g"] = 1m,
        ["kg"] = 1000m
    };

    private static readonly Dictionary<string, decimal> LengthToMm = new()
    {
        ["mm"] = 1m,
        ["cm"] = 10m,
        ["m"] = 1000m
    };

    private static readonly Dictionary<string, decimal> LifespanToMonths = new()
    {
        ["years"] = 12m,
        ["months"] = 1m,
        ["days"] = 12m / 365m
    };

    public static TraitData Parse(string text)
    {
        var (weightMin, weightMax) = ReadRange(text, "Range mass", MassToGrams);
        var (lengthMin, lengthMax) = ReadRange(text, "Range length", LengthToMm);
        var (lifeMin, lifeMax) = ReadLifespanRange(text, "wild", LifespanToMonths);
        if (lifeMin is null && lifeMax is null)
        {
            (lifeMin, lifeMax) = ReadLifespanRange(text, "captivity", LifespanToMonths);
        }


        return new TraitData
        {
            WeightGramsMin = weightMin is null ? null : UnitConversion.RoundGrams(weightMin.Value),
            WeightGramsMax = weightMax is null ? null : UnitConversion.RoundGrams(weightMax.Value),
            LengthMmMin = lengthMin is null ? null : UnitConversion.RoundLengthMillimetres(lengthMin.Value),
            LengthMmMax = lengthMax is null ? null : UnitConversion.RoundLengthMillimetres(lengthMax.Value),
            LifespanMonthsMin = lifeMin is null ? null : UnitConversion.RoundMonths(lifeMin.Value),
            LifespanMonthsMax = lifeMax is null ? null : UnitConversion.RoundMonths(lifeMax.Value)
        };
    }

    private static (decimal? Min, decimal? Max) ReadRange(string text, string label, Dictionary<string, decimal> units)
    {
        return ReadValues(Regex.Match(text, label + ":</strong>" + ValuePattern), units);
    }

    private static (decimal? Min, decimal? Max) ReadLifespanRange(string text, string status,
        Dictionary<string, decimal> units)
    {
        return ReadValues(
            Regex.Match(text, "Range lifespan<br><span>Status: " + status + "</span>:</strong>" + ValuePattern),
            units);
    }

    private static (decimal? Min, decimal? Max) ReadValues(Match match, Dictionary<string, decimal> units)
    {
        if (!match.Success)
        {
            return (null, null);
        }

        if (!units.TryGetValue(match.Groups[4].Value, out var factor))
        {
            return (null, null);
        }

        var first = ParseNumber(match.Groups[1].Value) * factor;
        if (match.Groups[2].Success)
        {
            return (first, ParseNumber(match.Groups[2].Value) * factor);
        }

        if (match.Groups[3].Value == "low")
        {
            return (first, null);
        }

        if (match.Groups[3].Value == "high")
        {
            return (null, first);
        }

        return (first, first);
    }

    private static decimal? ParseNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}