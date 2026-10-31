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
        
        TraitMerger.FillMissing(result, traits, "AnAge");
        
        Assert.Equal(1.5m, result.WeightKgMax);
        Assert.Equal(16, result.LifespanYearsMax);
    }

    [Fact]
    public void Does_not_fit_into_blank_fields()
    {
        var result = new WikipediaImportResult { WeightKgMin = 1.3m };
        var traits = new TraitData {WeightKgMax =  1.1m};
        
        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(1.3m, result.WeightKgMin);
        Assert.Null(result.WeightKgMax);
    }

    [Fact]
    public void Fills_missing_max_when_min_exists()
    {
        var result = new WikipediaImportResult { LifespanYearsMin = 10 };
        var traits = new TraitData { LifespanYearsMax = 31 };
        
        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(31, result.LifespanYearsMax);
    }

    [Fact]
    public void Warns_when_length_ranges_do_not_overlap()
    {
        var result = new WikipediaImportResult { SizeLengthCmMin = 20, SizeLengthCmMax = 30 };
        var traits = new TraitData { SizeLengthCmMax = 50 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Length: AnAge says 50, kept 20 - 30", warning);
    }

    [Fact]
    public void Warns_with_weight_label_when_weights_disagree()
    {
        var result = new WikipediaImportResult { WeightKgMin = 5, WeightKgMax = 8 };
        var traits = new TraitData { WeightKgMin = 20, WeightKgMax = 25 };

        TraitMerger.FillMissing(result, traits, "FishBase/SeaLifeBase");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Weight: FishBase/SeaLifeBase says 20 - 25, kept 5 - 8", warning);
    }

    [Fact]
    public void Warns_with_lifespan_label_when_lifespans_disagree()
    {
        var result = new WikipediaImportResult { LifespanYearsMax = 10 };
        var traits = new TraitData { LifespanYearsMin = 30 };

        TraitMerger.FillMissing(result, traits, "EOL");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Lifespan: EOL says 30, kept 10", warning);
    }

    [Fact]
    public void Does_not_warn_when_ranges_overlap()
    {
        var result = new WikipediaImportResult { SizeLengthCmMin = 20, SizeLengthCmMax = 30 };
        var traits = new TraitData { SizeLengthCmMin = 25, SizeLengthCmMax = 40 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Does_not_warn_when_ranges_only_touch()
    {
        var result = new WikipediaImportResult { WeightKgMin = 20, WeightKgMax = 30 };
        var traits = new TraitData { WeightKgMin = 30, WeightKgMax = 40 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Does_not_warn_when_one_side_is_empty()
    {
        var result = new WikipediaImportResult { LifespanYearsMax = 10 };
        var traits = new TraitData { SizeLengthCmMax = 50 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Warns_once_per_field_that_disagrees()
    {
        var result = new WikipediaImportResult { SizeLengthCmMax = 20, WeightKgMax = 5, LifespanYearsMax = 10 };
        var traits = new TraitData { SizeLengthCmMax = 50, WeightKgMax = 5, LifespanYearsMax = 40 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(2, result.Warnings.Count);
        Assert.StartsWith("Length:", result.Warnings[0]);
        Assert.StartsWith("Lifespan:", result.Warnings[1]);
    }
}