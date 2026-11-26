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
    private readonly AnimalRepository _repository;
    private readonly MissingTranslationFiller _filler;

    public AnimalsController(AnimalRepository repository, MissingTranslationFiller filler)
    {
        _repository = repository;
        _filler = filler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Animal>>> GetAll([FromQuery] string? lang)
    {
        var animals = await _repository.GetAllAsync(SupportedLanguages.Normalize(lang));
        return Ok(animals);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Animal>> GetById(int id, [FromQuery] string? lang)
    {
        var animal = await _repository.GetByIdAsync(id, SupportedLanguages.Normalize(lang));
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
        var images = await _repository.GetImagesByAnimalIdAsync(id);
        return Ok(images);
    }

    [HttpPost]
    [RequireRole(Role.Admin)]
    public async Task<ActionResult<Animal>> Create(CreateAnimalRequest request)
    {
        var animal = await _repository.CreateAsync(request);
        await _filler.FillAsync(animal.Id);
        return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
    }

    [HttpPut("{id:int}")]
    [RequireRole(Role.Admin)]
    public async Task<ActionResult<Animal>> Update(int id, CreateAnimalRequest request)
    {
        var animal = await _repository.UpdateAsync(id, request);
        if (animal is null)
        {
            return NotFound();
        }

        await _filler.FillAsync(id);
        return Ok(animal);
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
    [RequireRole(Role.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}