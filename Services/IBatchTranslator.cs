namespace AnimalDatabase.Api.Services;

public interface IBatchTranslator
{
    Task<string[]> TranslateAsync(string[] texts);
}