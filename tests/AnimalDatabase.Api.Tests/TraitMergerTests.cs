using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class TraitMergerTests
{
    [Fact]
    public void Fills_empty_fields_and_keeps_existing_ones()
    {
        var result = new WikipediaImportResult { WeightKgMax = 1.5m };
        var traits = new TraitData { WeightKgMax = 1.9m, LifespanYearsMax = 16 };
        
        TraitMerger.FillMissing(result, traits);
        
        Assert.Equal(1.5m, result.WeightKgMax);
        Assert.Equal(16, result.LifespanYearsMax);
    }
}