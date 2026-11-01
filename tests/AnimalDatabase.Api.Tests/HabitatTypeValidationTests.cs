using System.ComponentModel.DataAnnotations;
using AnimalDatabase.Api.Models;
using Xunit;

namespace AnimalDatabase.Api.Tests;

public class HabitatTypeValidationTests
{
    private static List<ValidationResult> Validate(string? habitatType)
    {
        var request = new CreateAnimalRequest { CommonName = "Okapi", HabitatType = habitatType };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Theory]
    [InlineData("forest")]
    [InlineData("grassland")]
    [InlineData("desert")]
    [InlineData("mountains")]
    [InlineData("wetland")]
    [InlineData("freshwater")]
    [InlineData("ocean")]
    [InlineData("deep sea")]
    [InlineData("underground")]
    public void Every_listed_habitat_type_is_accepted(string habitatType)
    {
        Assert.Empty(Validate(habitatType));
    }

    [Fact]
    public void An_animal_without_a_habitat_type_is_accepted()
    {
        Assert.Empty(Validate(null));
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("Forest")]
    [InlineData("rainforest")]
    [InlineData("")]
    public void Anything_outside_the_list_is_rejected(string habitatType)
    {
        var error = Assert.Single(Validate(habitatType));
        Assert.Contains(nameof(CreateAnimalRequest.HabitatType), error.MemberNames);
    }
}