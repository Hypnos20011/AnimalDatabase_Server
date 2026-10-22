using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class CreateSearchRequest
{
    [MaxLength(255)]
    public string? QueryText { get; set; }

    [Required]
    public int? AnimalId { get; set; }
}