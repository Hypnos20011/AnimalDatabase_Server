namespace AnimalDatabase.Api.Models;

public class TopSearch
{
    public int AnimalId { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SearchCount { get; set; }
}