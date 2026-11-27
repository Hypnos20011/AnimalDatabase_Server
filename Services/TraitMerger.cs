using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits, string sourceName)
    {
        var lengthMin = Round(traits.LengthMmMin, UnitConversion.RoundLengthMillimetres);
        var lengthMax = Round(traits.LengthMmMax, UnitConversion.RoundLengthMillimetres);
        var heightMin = Round(traits.HeightMmMin, UnitConversion.RoundLengthMillimetres);
        var heightMax = Round(traits.HeightMmMax, UnitConversion.RoundLengthMillimetres);
        var weightMin = Round(traits.WeightGramsMin, UnitConversion.RoundGrams);
        var weightMax = Round(traits.WeightGramsMax, UnitConversion.RoundGrams);
        var lifespanMin = Round(traits.LifespanMonthsMin, UnitConversion.RoundMonths);
        var lifespanMax = Round(traits.LifespanMonthsMax, UnitConversion.RoundMonths);

        if (Disagrees(result.LengthMmMin, result.LengthMmMax, lengthMin, lengthMax))
        {
            result.Warnings.Add(
                $"Length: {sourceName} says " +
                $"{Describe(lengthMin, lengthMax)}, " +
                $"kept {Describe(result.LengthMmMin, result.LengthMmMax)}");
        }

        (result.LengthMmMin, result.LengthMmMax) = MergePair(
            result.LengthMmMin,
            result.LengthMmMax,
            lengthMin,
            lengthMax
        );

        if (Disagrees(result.HeightMmMin, result.HeightMmMax, heightMin, heightMax))
        {
            result.Warnings.Add(
                $"Height: {sourceName} says " +
                $"{Describe(heightMin, heightMax)}, " +
                $"kept {Describe(result.HeightMmMin, result.HeightMmMax)}");
        }

        (result.HeightMmMin, result.HeightMmMax) = MergePair(
            result.HeightMmMin,
            result.HeightMmMax,
            heightMin,
            heightMax
        );

        if (Disagrees(result.WeightGramsMin, result.WeightGramsMax, weightMin, weightMax))
        {
            result.Warnings.Add(
                $"Weight: {sourceName} says " +
                $"{Describe(weightMin, weightMax)}, " +
                $"kept {Describe(result.WeightGramsMin, result.WeightGramsMax)}");
        }

        (result.WeightGramsMin, result.WeightGramsMax) = MergePair(
            result.WeightGramsMin,
            result.WeightGramsMax,
            weightMin,
            weightMax
        );


        if (Disagrees(result.LifespanMonthsMin, result.LifespanMonthsMax, lifespanMin, lifespanMax))
        {
            result.Warnings.Add(
                $"Lifespan: {sourceName} says " +
                $"{Describe(lifespanMin, lifespanMax)}, " +
                $"kept {Describe(result.LifespanMonthsMin, result.LifespanMonthsMax)}");
        }

        (result.LifespanMonthsMin, result.LifespanMonthsMax) = MergePair(
            result.LifespanMonthsMin,
            result.LifespanMonthsMax,
            lifespanMin,
            lifespanMax
        );

        result.Habitat ??= traits.Habitat;
    }

    private static decimal? Round(decimal? value, Func<decimal, decimal> round)
    {
        return value is null ? null : round(value.Value);
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