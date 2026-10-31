using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/searches")]
public class SearchesController : ControllerBase
{
    public const int DefaultTopDays = 30;
    public const int MaxTopDays = 365;

    private readonly ISearchRepository _repository;

    public SearchesController(ISearchRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.SearchLog)]
    public async Task<IActionResult> Create(CreateSearchRequest request)
    {
        var added = await _repository.AddAsync(request);
        if (!added) return NotFound();
        return NoContent();
    }

    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<TopSearch>>> GetTop(
        [FromQuery] int limit = 5,
        [FromQuery] int days = DefaultTopDays)
    {
        if (days < 0 || days > MaxTopDays)
            return BadRequest($"days must be between 1 and {MaxTopDays}, or 0 for all time.");

        var top = await _repository.GetTopAsync(Math.Clamp(limit, 1, 20), days == 0 ? null : days);
        return Ok(top);
    }
}