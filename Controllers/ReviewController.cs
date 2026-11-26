using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Localization;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/admin/review/animals")]
[RequireRole(Role.Admin)]
public class ReviewController : ControllerBase
{
    private readonly IAnimalRepository _repository;
    private readonly MissingTranslationFiller _filler;

    public ReviewController(IAnimalRepository repository, MissingTranslationFiller filler)
    {
        _repository = repository;
        _filler = filler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Animal>>> GetPending([FromQuery] string? lang)
    {
        var animals = await _repository.GetPendingAsync(SupportedLanguages.Normalize(lang));
        return Ok(animals);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _repository.ApproveAsync(id);
        if (result.Status == AnimalReviewStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == AnimalReviewStatus.NotPending)
        {
            return Conflict();
        }

        await _filler.FillAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectAnimalRequest? request)
    {
        var result = await _repository.RejectAsync(id, request?.Note?.Trim());
        return result.Status switch
        {
            AnimalReviewStatus.NotFound => NotFound(),
            AnimalReviewStatus.NotPending => Conflict(),
            _ => NoContent()
        };
    }
}