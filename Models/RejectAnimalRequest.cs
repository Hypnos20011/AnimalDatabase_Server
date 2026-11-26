using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class RejectAnimalRequest
{
    [MaxLength(500)] public string? Note { get; set; }
}