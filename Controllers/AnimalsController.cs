using AnimalDatabase.Api.Data;
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
    public async Task<ActionResult<IReadOnlyList<Animal>>> GetAll()
    {
        var animals = await _repository.GetAllAsync();
        return Ok(animals);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Animal>> GetById(int id)
    {
        var animal = await _repository.GetByIdAsync(id);
        if (animal is null)
        {
            return NotFound();
        }

        return Ok(animal);
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