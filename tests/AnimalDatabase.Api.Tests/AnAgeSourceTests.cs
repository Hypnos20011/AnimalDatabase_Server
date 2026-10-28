using AnimalDatabase.Api.Services;
namespace AnimalDatabase.Api.tests;

public class AnAgeSourceTests
{
    private static AnAgeSource Source() => new(Path.Combine(AppContext.BaseDirectory, "Data", "Reference", "anage_data.txt"));

    [Fact]
    public void Finds_the_fennec_fox_in_kilograms()
    {
        var traits = Source().Find("Vulpes zerda");
        
        Assert.NotNull(traits);
        Assert.Equal(1.25m, traits.WeightKgMax);
        Assert.Equal(16, traits.LifespanYearsMax);
    }

    [Fact]
    public void Ignores_case_and_extra_spaces()
    {
        Assert.NotNull(Source().Find(" vulPes   ZerDa  "));
    }

    [Fact]
    public void Returns_null_for_unknown_or_incomplete_names()
    {
        Assert.Null(Source().Find("Macrocheira kaempferi"));
        Assert.Null(Source().Find("vulp"));
    }

    [Fact]
    public void Find_naked_mole_rat()
    {
        var traits = Source().Find("Heterocephalus glaber");
        Assert.NotNull(traits);
        Assert.Equal(31, traits.LifespanYearsMax);
    }
}