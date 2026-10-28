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

    [Fact]
    public void Does_not_fit_into_blank_fields()
    {
        var result = new WikipediaImportResult { WeightKgMin = 1.3m };
        var traits = new TraitData {WeightKgMax =  1.1m};
        
        TraitMerger.FillMissing(result, traits);

        Assert.Equal(1.3m, result.WeightKgMin);
        Assert.Null(result.WeightKgMax);
    }

    [Fact]
    public void Checking_if_traitmerger_works()
    {
        var result = new WikipediaImportResult { LifespanYearsMin = 10 };
        var traits = new TraitData { LifespanYearsMax = 31 };
        
        TraitMerger.FillMissing(result, traits);

        Assert.Equal(31, result.LifespanYearsMax);
    }
}