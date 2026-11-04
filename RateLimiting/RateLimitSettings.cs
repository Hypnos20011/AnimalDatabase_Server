namespace AnimalDatabase.Api.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public int WindowSeconds { get; set; } = 60;
    public int GlobalPerWindow { get; set; } = 300;
    public int WikipediaImportPerWindow { get; set; } = 10;
    public int TranslatePerWindow { get; set; } = 10;
    public int SearchLogPerWindow { get; set; } = 30;
    public int AdminCheckPerWindow { get; set; } = 10;
}