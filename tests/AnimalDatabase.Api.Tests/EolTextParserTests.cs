using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class EolTextParserTests
{
    [Fact]
    public void Reads_a_full_range_and_converts_units()
    {
        var traits =
            EolTextParser.Parse(
                "<p><strong>Range mass:</strong> 85 to 165 g.</p><p><strong>Range length:</strong> 80 to 160 mm.</p>");

        Assert.Equal(85m, traits.WeightGramsMin);
        Assert.Equal(165m, traits.WeightGramsMax);
        Assert.Equal(80m, traits.LengthMmMin);
        Assert.Equal(160m, traits.LengthMmMax);
    }

    [Fact]
    public void High_only_fills_just_the_max()
    {
        var traits =
            EolTextParser.Parse(
                "<p><strong>Range mass:</strong> 16 to 20 kg.</p><p><strong>Range length:</strong> 3.7 (high)  m.</p>");
        Assert.Null(traits.LengthMmMin);
        Assert.Equal(3700m, traits.LengthMmMax);
    }

    [Fact]
    public void Text_without_ranges_gives_nothing()
    {
        var traits = EolTextParser.Parse("This animal is very unusual.");

        Assert.Null(traits.LengthMmMin);
        Assert.Null(traits.LengthMmMax);
    }

    [Fact]
    public void Reads_lifespan_range()
    {
        var traits = EolTextParser.Parse("<strong>Range lifespan<br><span>Status: captivity</span>:</strong> 22 to 31 years.");

        Assert.Equal(264m, traits.LifespanMonthsMin);
        Assert.Equal(372m, traits.LifespanMonthsMax);
    }

    [Fact]
    public void Proving_wild_wins_over_captivity()
    {
        var traits = EolTextParser.Parse("<strong>Range lifespan<br><span>Status: captivity</span>:</strong> 22 to 31 years.<strong>Range lifespan<br><span>Status: wild</span>:</strong> 8 (high) years.");

        Assert.Null(traits.LifespanMonthsMin);
        Assert.Equal(96m, traits.LifespanMonthsMax);
    }
}