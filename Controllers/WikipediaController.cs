using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/wikipedia")]
public class WikipediaController : ControllerBase
{
    private readonly WikipediaImporter _importer;
    private readonly AnAgeSource _anAge;
    private readonly SeaLifeBaseSource _seaLifeBase;

    public WikipediaController(WikipediaImporter importer, AnAgeSource anAge, SeaLifeBaseSource seaLifeBase)
    {
        _importer = importer;
        _anAge = anAge;
        _seaLifeBase = seaLifeBase;
    }

    [HttpGet("import")]
    [EnableRateLimiting(RateLimitPolicies.WikipediaImport)]
    [RequireAdminKey]
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

            if (result.ScientificName is not null)
            {
                var traits = _anAge.Find(result.ScientificName);
                if (traits is not null)
                {
                    TraitMerger.FillMissing(result, traits);
                }
                
                var seaTraits = _seaLifeBase.Find(result.ScientificName);
                if (seaTraits is not null)
                {
                    TraitMerger.FillMissing(result, seaTraits);
                }
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
