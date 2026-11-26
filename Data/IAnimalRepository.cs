using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;

namespace AnimalDatabase.Api.Data;

public interface IAnimalRepository
{
    Task<IReadOnlyList<Animal>> GetAllAsync(string language, Role? viewer);

    Task<Animal?> GetByIdAsync(int id, string language, Role? viewer);

    Task<IReadOnlyList<AnimalImage>> GetImagesByAnimalIdAsync(int animalId, Role? viewer);

    Task<bool> IsVisibleAsync(int id, Role? viewer);

    Task<Animal> CreateAsync(CreateAnimalRequest request, Role submitter);

    Task<AnimalWriteResult> UpdateAsync(int id, CreateAnimalRequest request, Role editor);

    Task<AnimalTexts?> GetTranslationAsync(int id, string language);

    Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts);

    Task<AnimalDeleteResult> DeleteAsync(int id, bool pendingTesterOnly);

    Task<IReadOnlyList<Animal>> GetPendingAsync(string language);

    Task<AnimalReviewResult> ApproveAsync(int id);

    Task<AnimalReviewResult> RejectAsync(int id, string? note);
}