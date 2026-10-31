using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class CreateAnimalImageRequest
{
    [Required]
    [RegularExpression("^(male|female|baby|unspecified)$")]
    public string Kind { get; set; } = string.Empty;

    [Required] [MaxLength(500)] [Url] public string Url { get; set; } = string.Empty;

    [MaxLength(200)] public string? Caption { get; set; }
}