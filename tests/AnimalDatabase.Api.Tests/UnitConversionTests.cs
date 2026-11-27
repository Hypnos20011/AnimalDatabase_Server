using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class UnitConversionTests
{
    [Fact]
    public void Converts_centimetres_and_millimetres_to_millimetres()
    {
        Assert.Equal(123.4m, UnitConversion.CentimetresToMillimetres(12.34m));
        Assert.Equal(12.34m, UnitConversion.MillimetresToMillimetres(12.34m));
    }

    [Fact]
    public void Converts_metres_inches_and_feet_to_millimetres()
    {
        Assert.Equal(1234m, UnitConversion.MetresToMillimetres(1.234m));
        Assert.Equal(25.4m, UnitConversion.InchesToMillimetres(1m));
        Assert.Equal(304.8m, UnitConversion.FeetToMillimetres(1m));
    }

    [Fact]
    public void Converts_kilograms_grams_and_pounds_to_grams()
    {
        Assert.Equal(136000000m, UnitConversion.KilogramsToGrams(136000m));
        Assert.Equal(2m, UnitConversion.GramsToGrams(2m));
        Assert.Equal(453.592m, UnitConversion.PoundsToGrams(1m));
    }

    [Fact]
    public void Converts_years_to_months()
    {
        Assert.Equal(15m, UnitConversion.YearsToMonths(1.25m));
    }

    [Fact]
    public void Rounds_half_values_away_from_zero_at_each_canonical_precision()
    {
        Assert.Equal(1.01m, UnitConversion.RoundLengthMillimetres(1.005m));
        Assert.Equal(1.235m, UnitConversion.RoundGrams(1.2345m));
        Assert.Equal(-1.235m, UnitConversion.RoundGrams(-1.2345m));
        Assert.Equal(0.1m, UnitConversion.RoundMonths(0.05m));
    }
}