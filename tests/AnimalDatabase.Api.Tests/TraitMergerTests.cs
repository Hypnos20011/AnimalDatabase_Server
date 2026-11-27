using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class TraitMergerTests
{
    [Fact]
    public void Fills_empty_fields_and_keeps_existing_ones()
    {
        var result = new WikipediaImportResult { WeightGramsMax = 1500m };
        var traits = new TraitData { WeightGramsMax = 1900m, LifespanMonthsMax = 192m };
        
        TraitMerger.FillMissing(result, traits, "AnAge");
        
        Assert.Equal(1500m, result.WeightGramsMax);
        Assert.Equal(192m, result.LifespanMonthsMax);
    }

    [Fact]
    public void Does_not_fit_into_blank_fields()
    {
        var result = new WikipediaImportResult { WeightGramsMin = 1300m };
        var traits = new TraitData { WeightGramsMax = 1100m };
        
        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(1300m, result.WeightGramsMin);
        Assert.Null(result.WeightGramsMax);
    }

    [Fact]
    public void Fills_missing_max_when_min_exists()
    {
        var result = new WikipediaImportResult { LifespanMonthsMin = 120m };
        var traits = new TraitData { LifespanMonthsMax = 372m };
        
        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(372m, result.LifespanMonthsMax);
    }

    [Fact]
    public void Warns_when_length_ranges_do_not_overlap()
    {
        var result = new WikipediaImportResult { LengthMmMin = 200, LengthMmMax = 300 };
        var traits = new TraitData { LengthMmMax = 500 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Length: AnAge says 500, kept 200 - 300", warning);
    }

    [Fact]
    public void Warns_with_weight_label_when_weights_disagree()
    {
        var result = new WikipediaImportResult { WeightGramsMin = 5000, WeightGramsMax = 8000 };
        var traits = new TraitData { WeightGramsMin = 20000, WeightGramsMax = 25000 };

        TraitMerger.FillMissing(result, traits, "FishBase/SeaLifeBase");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Weight: FishBase/SeaLifeBase says 20000 - 25000, kept 5000 - 8000", warning);
    }

    [Fact]
    public void Warns_with_lifespan_label_when_lifespans_disagree()
    {
        var result = new WikipediaImportResult { LifespanMonthsMax = 120 };
        var traits = new TraitData { LifespanMonthsMin = 360 };

        TraitMerger.FillMissing(result, traits, "EOL");

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Lifespan: EOL says 360, kept 120", warning);
    }

    [Fact]
    public void Does_not_warn_when_ranges_overlap()
    {
        var result = new WikipediaImportResult { LengthMmMin = 200, LengthMmMax = 300 };
        var traits = new TraitData { LengthMmMin = 250, LengthMmMax = 400 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Does_not_warn_when_ranges_only_touch()
    {
        var result = new WikipediaImportResult { WeightGramsMin = 20000, WeightGramsMax = 30000 };
        var traits = new TraitData { WeightGramsMin = 30000, WeightGramsMax = 40000 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Does_not_warn_when_one_side_is_empty()
    {
        var result = new WikipediaImportResult { LifespanMonthsMax = 120 };
        var traits = new TraitData { LengthMmMax = 500 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Warns_once_per_field_that_disagrees()
    {
        var result = new WikipediaImportResult { LengthMmMax = 200, WeightGramsMax = 5000, LifespanMonthsMax = 120 };
        var traits = new TraitData { LengthMmMax = 500, WeightGramsMax = 5000, LifespanMonthsMax = 480 };

        TraitMerger.FillMissing(result, traits, "AnAge");

        Assert.Equal(2, result.Warnings.Count);
        Assert.StartsWith("Length:", result.Warnings[0]);
        Assert.StartsWith("Lifespan:", result.Warnings[1]);
    }

    [Fact]
    public void Rounds_source_values_to_canonical_precision()
    {
        var result = new WikipediaImportResult();
        var traits = new TraitData { LengthMmMax = 12.345m, WeightGramsMax = 1.2345m };

        TraitMerger.FillMissing(result, traits, "FishBase/SeaLifeBase");

        Assert.Equal(12.35m, result.LengthMmMax);
        Assert.Equal(1.235m, result.WeightGramsMax);
    }

    [Fact]
    public void A_two_gram_weight_is_preserved()
    {
        var result = new WikipediaImportResult();

        TraitMerger.FillMissing(result, new TraitData { WeightGramsMax = 2m }, "AnAge");

        Assert.Equal(2m, result.WeightGramsMax);
    }

    [Fact]
    public void Leaves_the_shared_source_data_unchanged()
    {
        var traits = new TraitData { LengthMmMax = 12.34m, WeightGramsMax = 2m };

        TraitMerger.FillMissing(new WikipediaImportResult(), traits, "AnAge");

        Assert.Equal(12.34m, traits.LengthMmMax);
        Assert.Equal(2m, traits.WeightGramsMax);
    }
}