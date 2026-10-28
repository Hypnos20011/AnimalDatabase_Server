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

        Assert.Equal(0.085m, traits.WeightKgMin);
        Assert.Equal(0.165m, traits.WeightKgMax);
        Assert.Equal(8m, traits.SizeLengthCmMin);
        Assert.Equal(16m, traits.SizeLengthCmMax);
    }

    [Fact]
    public void High_only_fills_just_the_max()
    {
        var traits =
            EolTextParser.Parse(
                "<p><strong>Range mass:</strong> 16 to 20 kg.</p><p><strong>Range length:</strong> 3.7 (high)  m.</p>");
        Assert.Null(traits.SizeLengthCmMin);
        Assert.Equal(370m, traits.SizeLengthCmMax);
    }

    [Fact]
    public void Text_without_ranges_gives_nothing()
    {
        var traits = EolTextParser.Parse("This animal is very unusual.");

        Assert.Null(traits.SizeLengthCmMin);
        Assert.Null(traits.SizeLengthCmMax);
    }
}