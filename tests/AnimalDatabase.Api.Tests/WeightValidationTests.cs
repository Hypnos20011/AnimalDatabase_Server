using System.ComponentModel.DataAnnotations;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Tests;

public class WeightValidationTests
{
    private static List<ValidationResult> Validate(decimal weightKg)
    {
        var request = new CreateAnimalRequest { CommonName = "Blue whale", WeightKgMax = weightKg };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Fact]
    public void A_blue_whale_weight_is_accepted()
    {
        Assert.Empty(Validate(136000m));
    }

    [Fact]
    public void A_weight_over_the_column_limit_is_rejected()
    {
        Assert.NotEmpty(Validate(10000000m));
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
    [InlineData(10, 5, 1)]
    [InlineData(5, 10, 0)]
    [InlineData(5, 5, 0)]
    public void A_minimum_above_the_maximum_is_rejected(int min, int max, int errors)
    {
        var request = new CreateAnimalRequest { CommonName = "Okapi", LifespanYearsMin = min, LifespanYearsMax = max };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.Equal(errors, results.Count);
    }
}
