using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/searches")]
public class SearchesController : ControllerBase
{
    private readonly SearchRepository _repository;

    public SearchesController(SearchRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSearchRequest request)
    {
        var added = await _repository.AddAsync(request);
        if (!added)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<TopSearch>>> GetTop([FromQuery] int limit = 5)
    {
        var top = await _repository.GetTopAsync(Math.Clamp(limit, 1, 20));
        return Ok(top);
    }
}