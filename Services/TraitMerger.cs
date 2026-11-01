using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits, string sourceName)
    {
        if (Disagrees(result.SizeLengthCmMin, result.SizeLengthCmMax, traits.SizeLengthCmMin, traits.SizeLengthCmMax))
        {
            result.Warnings.Add(
                $"Length: {sourceName} says " +
                $"{Describe(traits.SizeLengthCmMin, traits.SizeLengthCmMax)}, " +
                $"kept {Describe(result.SizeLengthCmMin, result.SizeLengthCmMax)}");
        }

        (result.SizeLengthCmMin, result.SizeLengthCmMax) = MergePair(
            result.SizeLengthCmMin,
            result.SizeLengthCmMax,
            traits.SizeLengthCmMin,
            traits.SizeLengthCmMax
        );


        if (Disagrees(result.WeightKgMin, result.WeightKgMax, traits.WeightKgMin, traits.WeightKgMax))
        {
            result.Warnings.Add(
                $"Weight: {sourceName} says " +
                $"{Describe(traits.WeightKgMin, traits.WeightKgMax)}, " +
                $"kept {Describe(result.WeightKgMin, result.WeightKgMax)}");
        }

        (result.WeightKgMin, result.WeightKgMax) = MergePair(
            result.WeightKgMin,
            result.WeightKgMax,
            traits.WeightKgMin,
            traits.WeightKgMax
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