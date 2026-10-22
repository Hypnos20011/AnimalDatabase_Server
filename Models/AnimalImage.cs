namespace AnimalDatabase.Api.Models;

public class AnimalImage
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}