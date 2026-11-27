using System.ComponentModel.DataAnnotations;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Tests;

public class WeightValidationTests
{
    private static List<ValidationResult> Validate(decimal weightGrams)
    {
        var request = new CreateAnimalRequest { CommonName = "Blue whale", WeightGramsMax = weightGrams };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Fact]
    public void A_blue_whale_weight_is_accepted()
    {
        Assert.Empty(Validate(136000000m));
    }

    [Fact]
    public void A_weight_over_the_column_limit_is_rejected()
    {
        Assert.NotEmpty(Validate(10000000000m));
    }

    [Fact]
    public void A_profile_too_long_for_the_database_is_rejected()
    {
        var request = new CreateAnimalRequest { CommonName = "Okapi", Profile = new string('a', 20001) };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData(120, 60, 1)]
    [InlineData(60, 120, 0)]
    [InlineData(60, 60, 0)]
    public void A_minimum_above_the_maximum_is_rejected(decimal min, decimal max, int errors)
    {
        var request = new CreateAnimalRequest
        {
            CommonName = "Okapi",
            LifespanMonthsMin = min,
            LifespanMonthsMax = max
        };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Equal(errors, results.Count);
    }

    [Fact]
    public void Canonical_measurement_column_limits_are_accepted()
    {
        var request = new CreateAnimalRequest
        {
            CommonName = "Okapi",
            LengthMmMin = 9999999.99m,
            HeightMmMax = 9999999.99m,
            WeightGramsMax = 9999999999.999m,
            LifespanMonthsMax = 99999.9m
        };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Empty(results);
    }

    [Fact]
    public void Canonical_measurement_values_above_the_api_limits_are_rejected()
    {
        var request = new CreateAnimalRequest
        {
            CommonName = "Okapi",
            LengthMmMin = 10000000m,
            HeightMmMax = 10000000m,
            WeightGramsMax = 10000000000m,
            LifespanMonthsMax = 100000m
        };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Equal(4, results.Count);
    }
}
