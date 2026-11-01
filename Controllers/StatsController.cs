using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsRepository _repository;

    public StatsController(IStatsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<AnimalStats>> Get()
    {
        return Ok(await _repository.GetStatsAsync());
    }
}