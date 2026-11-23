using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public interface IAnimalTranslationStore
{
    Task<AnimalTexts?> GetTranslationAsync(int id, string language);

    Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts);

    Task<EnglishAnimal?> GetEnglishAsync(int id);

    Task<IReadOnlyList<EnglishAnimalRecord>> ListEnglishAsync();
}
