using System.Net;
using System.Text.Json;
using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalDatabase.Api.Tests;

public class StatsControllerTests
{
    private sealed class FakeStatsRepository : IStatsRepository
    {
        public AnimalStats Result { get; } = new()
        {
            TotalAnimals = 3,
            DomesticatedAnimals = 1,
            ByClass = new() { ["mammal"] = 2, ["unknown"] = 1 },
            ByIucnStatus = new() { ["endangered"] = 1, ["unknown"] = 2 },
            ByHabitatType = new() { ["deep sea"] = 1, ["forest"] = 1, ["unknown"] = 1 }
        };

        public Task<AnimalStats> GetStatsAsync() => Task.FromResult(Result);
    }

    [Fact]
    public async Task Get_returns_the_stats_from_the_repository()
    {
        var repository = new FakeStatsRepository();
        var controller = new StatsController(repository);

        var response = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(repository.Result, ok.Value);
    }

    [Fact]
    public async Task Stats_endpoint_sends_every_number_as_json()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStatsRepository, FakeStatsRepository>()));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3, root.GetProperty("totalAnimals").GetInt32());
        Assert.Equal(1, root.GetProperty("domesticatedAnimals").GetInt32());
        Assert.Equal(2, root.GetProperty("byClass").GetProperty("mammal").GetInt32());
        Assert.Equal(1, root.GetProperty("byClass").GetProperty("unknown").GetInt32());
        Assert.Equal(1, root.GetProperty("byIucnStatus").GetProperty("endangered").GetInt32());
        Assert.Equal(2, root.GetProperty("byIucnStatus").GetProperty("unknown").GetInt32());
        var habitats = root.GetProperty("byHabitatType");
        Assert.Equal(1, habitats.GetProperty("deep sea").GetInt32());
        Assert.Equal(1, habitats.GetProperty("forest").GetInt32());
        Assert.Equal(1, habitats.GetProperty("unknown").GetInt32());
    }

    [Fact]
    public void Stats_repository_is_registered()
    {
        using var factory = new ApiFactory();

        var repository = factory.Services.GetRequiredService<IStatsRepository>();

        Assert.IsType<StatsRepository>(repository);
    }
}