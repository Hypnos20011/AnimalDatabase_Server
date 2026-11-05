using System.Net;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalDatabase.Api.Controllers;

[Route("api/translate")]
[ApiController]
public class TranslateController : ControllerBase
{
    private readonly AnimalTextTranslator _translator;

    public TranslateController(AnimalTextTranslator translator)
    {
        _translator = translator;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Translate)]
    [RequireAdminKey]
    public async Task<ActionResult<AnimalTexts>> Translate([FromBody] AnimalTexts english)
    {
        var length = (english.CommonName?.Length ?? 0) + (english.Habitat?.Length ?? 0) +
                     (english.RangeRegion?.Length ?? 0) + (english.Profile?.Length ?? 0);
        if (length > 3000)
        {
            return BadRequest("Too many characters!");
        }

        try
        {
            var result = await _translator.TranslateAsync(english);
            return Ok(result);
        }
        catch (HttpRequestException e) when (e.StatusCode == (HttpStatusCode)456)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The character credit is used up");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Could not reach the translation service");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "The translation service took too long to answer");
        }
    }
}