using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits, string sourceName)
    {
        var lengthMin = Round(traits.SizeLengthCmMin, WikipediaImporter.RoundLength);
        var lengthMax = Round(traits.SizeLengthCmMax, WikipediaImporter.RoundLength);
        var weightMin = Round(traits.WeightKgMin, WikipediaImporter.RoundWeight);
        var weightMax = Round(traits.WeightKgMax, WikipediaImporter.RoundWeight);

        if (Disagrees(result.SizeLengthCmMin, result.SizeLengthCmMax, lengthMin, lengthMax))
        {
            result.Warnings.Add(
                $"Length: {sourceName} says " +
                $"{Describe(lengthMin, lengthMax)}, " +
                $"kept {Describe(result.SizeLengthCmMin, result.SizeLengthCmMax)}");
        }

        (result.SizeLengthCmMin, result.SizeLengthCmMax) = MergePair(
            result.SizeLengthCmMin,
            result.SizeLengthCmMax,
            lengthMin,
            lengthMax
        );


        if (Disagrees(result.WeightKgMin, result.WeightKgMax, weightMin, weightMax))
        {
            result.Warnings.Add(
                $"Weight: {sourceName} says " +
                $"{Describe(weightMin, weightMax)}, " +
                $"kept {Describe(result.WeightKgMin, result.WeightKgMax)}");
        }

        (result.WeightKgMin, result.WeightKgMax) = MergePair(
            result.WeightKgMin,
            result.WeightKgMax,
            weightMin,
            weightMax
        );


        if (Disagrees(result.LifespanYearsMin, result.LifespanYearsMax, traits.LifespanYearsMin,
                traits.LifespanYearsMax))
        {
            result.Warnings.Add(
                $"Lifespan: {sourceName} says " +
                $"{Describe(traits.LifespanYearsMin, traits.LifespanYearsMax)}, " +
                $"kept {Describe(result.LifespanYearsMin, result.LifespanYearsMax)}");
        }

        (result.LifespanYearsMin, result.LifespanYearsMax) = MergePair(
            result.LifespanYearsMin,
            result.LifespanYearsMax,
            traits.LifespanYearsMin,
            traits.LifespanYearsMax
        );

        result.Habitat ??= traits.Habitat;
    }

    private static decimal? Round(decimal? value, Func<decimal, decimal> round)
    {
        return value is null ? null : round(value.Value);
    }

    private static (int? Min, int? Max) MergePair(int? existingMin, int? existingMax,
        int? sourceMin, int? sourceMax)
    {
        return (existingMin, existingMax) switch
        {
            (null, null) => (sourceMin, sourceMax),
            (not null, null) when sourceMax >= existingMin => (existingMin, sourceMax),
            (null, not null) when sourceMin <= existingMax => (sourceMin, existingMax),
            _ => (existingMin, existingMax)
        };
    }

    private static (decimal? min, decimal? max) MergePair(decimal? existingMin, decimal? existingMax,
        decimal? sourceMin, decimal? sourceMax)
    {
        return (existingMin, existingMax) switch
        {
            (null, null) => (sourceMin, sourceMax),
            (not null, null) when sourceMax >= existingMin => (existingMin, sourceMax),
            (null, not null) when sourceMin <= existingMax => (sourceMin, existingMax),
            _ => (existingMin, existingMax)
        };
    }

    private static bool Disagrees(decimal? existingMin, decimal? existingMax, decimal? sourceMin, decimal? sourceMax)
    {
        if ((existingMin == null && existingMax == null) || (sourceMin == null && sourceMax == null))
        {
            return false;
        }

        var existingLow = existingMin ?? existingMax;
        var existingHigh = existingMax ?? existingMin;
        var sourceLow = sourceMin ?? sourceMax;
        var sourceHigh = sourceMax ?? sourceMin;

        return existingHigh < sourceLow || sourceHigh < existingLow;
    }

    private static string Describe(decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue && min != max)
        {
            return $"{min} - {max}";
        }

        return $"{min ?? max}";
    }
}