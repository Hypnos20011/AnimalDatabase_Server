using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Localization;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/animals")]
public class AnimalsController : ControllerBase
{
    private readonly IAnimalRepository _repository;
    private readonly MissingTranslationFiller _filler;

    public AnimalsController(IAnimalRepository repository, MissingTranslationFiller filler)
    {
        _repository = repository;
        _filler = filler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Animal>>> GetAll([FromQuery] string? lang)
    {
        var animals = await _repository.GetAllAsync(SupportedLanguages.Normalize(lang), HttpContext.GetViewerRole());
        return Ok(animals);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Animal>> GetById(int id, [FromQuery] string? lang)
    {
        var animal = await _repository.GetByIdAsync(id, SupportedLanguages.Normalize(lang), HttpContext.GetViewerRole());
        if (animal is null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpGet("{id:int}/translations/{language}")]
    public async Task<ActionResult<AnimalTexts>> GetTranslation(int id, string language)
    {
        if (!SupportedLanguages.All.Contains(language) ||
            language.Equals(SupportedLanguages.Default, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid language");
        }

        var viewer = HttpContext.GetViewerRole();
        if (!await _repository.IsVisibleAsync(id, viewer))
        {
            return NotFound();
        }

        var texts = await _repository.GetTranslationAsync(id, SupportedLanguages.Normalize(language));
        if (texts is null)
        {
            return NotFound();
        }

        return Ok(texts);
    }

    [HttpGet("{id:int}/images")]
    public async Task<ActionResult<IReadOnlyList<AnimalImage>>> GetImages(int id)
    {
        var viewer = HttpContext.GetViewerRole();
        if (!await _repository.IsVisibleAsync(id, viewer))
        {
            return NotFound();
        }

        var images = await _repository.GetImagesByAnimalIdAsync(id, viewer);
        return Ok(images);
    }

    [HttpPost]
    [RequireRole(Role.Tester)]
    public async Task<ActionResult<Animal>> Create(CreateAnimalRequest request)
    {
        var role = (Role)HttpContext.Items[RequireRoleAttribute.RoleItemKey]!;
        var animal = await _repository.CreateAsync(request, role);
        if (animal.Status == AnimalStatus.Approved)
        {
            await _filler.FillAsync(animal.Id);
        }

        return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
    }

    [HttpPut("{id:int}")]
    [RequireRole(Role.Tester)]
    public async Task<ActionResult<Animal>> Update(int id, CreateAnimalRequest request)
    {
        var role = (Role)HttpContext.Items[RequireRoleAttribute.RoleItemKey]!;
        var result = await _repository.UpdateAsync(id, request, role);
        if (result.Status == AnimalWriteStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == AnimalWriteStatus.Forbidden)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Editing an approved animal needs admin approval.");
        }

        if (result.Animal?.Status == AnimalStatus.Approved)
        {
            await _filler.FillAsync(id);
        }

        return Ok(result.Animal);
    }

    [HttpPut("{id:int}/translations/{language}")]
    [RequireRole(Role.Admin)]
    public async Task<IActionResult> SaveTranslation(int id, string language, AnimalTexts texts)
    {
        if (!SupportedLanguages.All.Contains(language) ||
            language.Equals(SupportedLanguages.Default, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid language");
        }

        var saved = await _repository.SaveTranslationAsync(id, SupportedLanguages.Normalize(language), texts);
        if (!saved)
        {
            return NotFound();
        }

        await _filler.FillAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/translate-missing")]
    [RequireRole(Role.Admin)]
    public async Task<ActionResult<MissingTranslationReport>> TranslateMissing(int id)
    {
        if (await _filler.ReportAsync(id) is null)
        {
            return NotFound();
        }

        await _filler.FillAsync(id);
        var remaining = await _filler.ReportAsync(id);
        if (remaining is null)
        {
            return NotFound();
        }

        return Ok(remaining);
    }

    [HttpDelete("{id:int}")]
    [RequireRole(Role.Tester)]
    public async Task<IActionResult> Delete(int id)
    {
        var role = (Role)HttpContext.Items[RequireRoleAttribute.RoleItemKey]!;
        var result = await _repository.DeleteAsync(id, role == Role.Tester);
        if (result.Status == AnimalDeleteStatus.NotFound)
        {
            return NotFound();
        }

        return result.Status == AnimalDeleteStatus.Forbidden
            ? StatusCode(StatusCodes.Status403Forbidden)
            : NoContent();
    }
}