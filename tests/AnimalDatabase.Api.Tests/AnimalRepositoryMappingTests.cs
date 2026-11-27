using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Tests;

public class AnimalRepositoryMappingTests
{
    [Fact]
    public void Maps_canonical_measurement_request_fields_to_dapper_parameters()
    {
        var request = new CreateAnimalRequest
        {
            LengthMmMin = 12.34m,
            LengthMmMax = 56.78m,
            HeightMmMin = 9.01m,
            HeightMmMax = 10.02m,
            WeightGramsMin = 2m,
            WeightGramsMax = 136000000m,
            LifespanMonthsMin = 0.1m,
            LifespanMonthsMax = 99999.9m
        };

        var parameters = AnimalRepository.ToParameters(request);

        Assert.Equal(request.LengthMmMin, parameters.Get<decimal?>("LengthMmMin"));
        Assert.Equal(request.LengthMmMax, parameters.Get<decimal?>("LengthMmMax"));
        Assert.Equal(request.HeightMmMin, parameters.Get<decimal?>("HeightMmMin"));
        Assert.Equal(request.HeightMmMax, parameters.Get<decimal?>("HeightMmMax"));
        Assert.Equal(request.WeightGramsMin, parameters.Get<decimal?>("WeightGramsMin"));
        Assert.Equal(request.WeightGramsMax, parameters.Get<decimal?>("WeightGramsMax"));
        Assert.Equal(request.LifespanMonthsMin, parameters.Get<decimal?>("LifespanMonthsMin"));
        Assert.Equal(request.LifespanMonthsMax, parameters.Get<decimal?>("LifespanMonthsMax"));
    }
}