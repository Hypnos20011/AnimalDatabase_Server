using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Localization;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/animals")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalRepository _repository;

    public AnimalsController(AnimalRepository repository)
    {
        _repository = repository;
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
    [RequireAdminKey]
    public async Task<ActionResult<Animal>> Create(CreateAnimalRequest request)
    {
        var animal = await _repository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = animal.Id }, animal);
    }

    [HttpPut("{id:int}")]
    [RequireAdminKey]
    public async Task<ActionResult<Animal>> Update(int id, CreateAnimalRequest request)
    {
        var animal = await _repository.UpdateAsync(id, request);
        if (animal is null)
        {
            return NotFound();
        }

        return Ok(animal);
    }

    [HttpPut("{id:int}/translations/{language}")]
    [RequireAdminKey]
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

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequireAdminKey]
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