using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.tests.AnimalDatabase.Api.Tests;

public class SeaLifeBaseSourceTests
{
    private static SeaLifeBaseSource Source() => new(
        Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "sealifebase_data.txt"),
        Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "fishbase_data.txt"));

    [Fact]
    public void Finds_the_european_lobster()
    {
        var traits = Source().Find("Homarus gammarus");

        Assert.NotNull(traits);
        Assert.Equal(65m, traits.SizeLengthCmMax);
        Assert.Equal(6m, traits.WeightKgMax);
        Assert.Equal(72, traits.LifespanYearsMax);
    }

    [Fact]
    public void Ignores_a_width_as_length()
    {
        var traits = Source().Find("Macrocheira kaempferi");

        Assert.NotNull(traits);
        Assert.Null(traits.SizeLengthCmMax);
        Assert.Equal(13.6m, traits.WeightKgMax);
    }

    [Fact]
    public void Drops_an_impossible_weight()
    {
        var traits = Source().Find("Micropterus chattahoochae");

        Assert.NotNull(traits);
        Assert.Null(traits.WeightKgMax);
    }

    [Fact]
    public void Returns_null_for_unknown_names()
    {
        Assert.Null(Source().Find("Vulpes zerda"));
    }
    
    
}