using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAnimalTranslationStore _store;

    public AdminController(IAnimalTranslationStore store)
    {
        _store = store;
    }

    [HttpGet("check")]
    [EnableRateLimiting(RateLimitPolicies.AdminCheck)]
    [RequireAdminKey]
    public IActionResult Check()
    {
        return NoContent();
    }

    [HttpGet("missing-translations")]
    [RequireAdminKey]
    public async Task<ActionResult<IReadOnlyList<MissingTranslationReport>>> MissingTranslations()
    {
        var animals = await _store.ListEnglishAsync();
        return Ok(TranslationGaps.Reports(animals));
    }
}