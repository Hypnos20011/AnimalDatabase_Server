using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class AnimalTexts
{
    [MaxLength(120)] public string? CommonName { get; set; }
    [MaxLength(255)] public string? Habitat { get; set; }
    [MaxLength(255)] public string? RangeRegion { get; set; }
    public string? Profile { get; set; }
}