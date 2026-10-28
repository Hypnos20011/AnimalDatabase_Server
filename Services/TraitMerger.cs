using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits)
    {
        (result.SizeLengthCmMin, result.SizeLengthCmMax) = MergePair(
            result.SizeLengthCmMin,
            result.SizeLengthCmMax,
            traits.SizeLengthCmMin,
            traits.SizeLengthCmMax
        );

        (result.WeightKgMin, result.WeightKgMax) = MergePair(result.WeightKgMin, result.WeightKgMax, traits.WeightKgMin,
            traits.WeightKgMax);

        (result.LifespanYearsMin, result.LifespanYearsMax) = MergePair(result.LifespanYearsMin, result.LifespanYearsMax,
            traits.LifespanYearsMin, traits.LifespanYearsMax);

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
}