using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Tests;

public class SearchesControllerTests
{
    private sealed class FakeSearchRepository : ISearchRepository
    {
        public List<(int Limit, int? Days)> TopCalls { get; } = new();

        public IReadOnlyList<TopSearch> Result { get; set; } =
            new[] { new TopSearch { AnimalId = 2, CommonName = "Okapi", SearchCount = 3 } };

        public Task<bool> AddAsync(CreateSearchRequest request) => Task.FromResult(true);

        public Task<IReadOnlyList<TopSearch>> GetTopAsync(int limit, int? days)
        {
            TopCalls.Add((limit, days));
            return Task.FromResult(Result);
        }
    }

    [Fact]
    public async Task Default_window_is_30_days_and_limit_5()
    {
        var repository = new FakeSearchRepository();
        var controller = new SearchesController(repository);

        var response = await controller.GetTop();

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(repository.Result, ok.Value);
        Assert.Equal(new[] { (5, (int?)30) }, repository.TopCalls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task Days_inside_the_range_are_passed_through(int days)
    {
        var repository = new FakeSearchRepository();
        var controller = new SearchesController(repository);

        var response = await controller.GetTop(5, days);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(new[] { (5, (int?)days) }, repository.TopCalls);
    }

    [Fact]
    public async Task Zero_days_means_all_time()
    {
        var repository = new FakeSearchRepository();
        var controller = new SearchesController(repository);

        var response = await controller.GetTop(5, 0);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(new[] { (5, (int?)null) }, repository.TopCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task Days_outside_the_range_return_bad_request(int days)
    {
        var repository = new FakeSearchRepository();
        var controller = new SearchesController(repository);

        var response = await controller.GetTop(5, days);

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("between 1 and 365", bad.Value as string);
        Assert.Empty(repository.TopCalls);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    [InlineData(50, 20)]
    public async Task Limit_keeps_being_clamped_to_1_through_20(int limit, int expected)
    {
        var repository = new FakeSearchRepository();
        var controller = new SearchesController(repository);

        await controller.GetTop(limit, 7);

        Assert.Equal(new[] { (expected, (int?)7) }, repository.TopCalls);
    }
}
