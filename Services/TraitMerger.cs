using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public static class TraitMerger
{
    public static void FillMissing(WikipediaImportResult result, TraitData traits)
    {
        if (result.SizeLengthCmMin is null && result.SizeLengthCmMax is null)
        {
            result.SizeLengthCmMin = traits.SizeLengthCmMin;
            result.SizeLengthCmMax = traits.SizeLengthCmMax;
        }

        if (result.WeightKgMax is null && result.WeightKgMin is null)
        {
            result.WeightKgMin = traits.WeightKgMin;
            result.WeightKgMax = traits.WeightKgMax;
        }

        if (result.LifespanYearsMax is null && result.LifespanYearsMin is null)
        {
            result.LifespanYearsMin = traits.LifespanYearsMin;
            result.LifespanYearsMax = traits.LifespanYearsMax;
        }

        result.Habitat ??= traits.Habitat;
    }
}