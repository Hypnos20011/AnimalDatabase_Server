using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/wikipedia")]
public class WikipediaController : ControllerBase
{
    private readonly WikipediaImporter _importer;

    public WikipediaController(WikipediaImporter importer)
    {
        _importer = importer;
    }

    [HttpGet("import")]
    public async Task<ActionResult<WikipediaImportResult>> Import([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 300)
        {
            return BadRequest("Type an animal name or paste a Wikipedia link.");
        }

        try
        {
            var isLink = q.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase);
            if (isLink && !WikipediaImporter.TryParseUrl(q, out _, out _))
            {
                return BadRequest("That isn't a Wikipedia article link.");
            }

            var result = isLink && WikipediaImporter.TryParseUrl(q, out var language, out var title)
                ? await _importer.ImportAsync(language, title)
                : await _importer.ImportByNameAsync(q);
            if (result is null)
            {
                return NotFound("Couldn't find a Wikipedia article for that.");
            }
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Could not reach Wikipedia.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Wikipedia took too long to answer.");
        }
    }
}
