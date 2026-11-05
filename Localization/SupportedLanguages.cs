namespace AnimalDatabase.Api.Localization;

public static class SupportedLanguages
{
    public const string Default = "en";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "en", "de", "fr" };

    public static string Normalize(string? language)
    {
        return string.IsNullOrWhiteSpace(language) || !All.Contains(language) ? Default : language.ToLowerInvariant();
    }
}