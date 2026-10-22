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
    public async Task<ActionResult<WikipediaImportResult>> Import([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !WikipediaImporter.TryParseUrl(url, out var language, out var title))
        {
            return BadRequest("That isn't a Wikipedia article link.");
        }

        try
        {
            var result = await _importer.ImportAsync(language, title);
            if (result is null)
            {
                return NotFound("No Wikipedia article found for that link.");
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
