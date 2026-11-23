namespace AnimalDatabase.Api.Models;

public class MissingTranslationReport
{
    public int Id { get; set; }
    public string CommonName { get; set; } = "";
    public IReadOnlyList<MissingTranslationLanguage> Languages { get; set; } = [];
}

public class MissingTranslationLanguage
{
    public string Language { get; set; } = "";
    public IReadOnlyList<string> Fields { get; set; } = [];
}
