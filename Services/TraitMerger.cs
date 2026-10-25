using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits)
    {
        result.SizeLengthCmMin ??= traits.SizeLengthCmMin;
        result.SizeLengthCmMax ??= traits.SizeLengthCmMax;
        result.WeightKgMin ??= traits.WeightKgMin;
        result.WeightKgMax ??= traits.WeightKgMax;
        result.LifespanYearsMin ??= traits.LifespanYearsMin;
        result.LifespanYearsMax ??= traits.LifespanYearsMax;
        result.Habitat ??= traits.Habitat;
    }
}