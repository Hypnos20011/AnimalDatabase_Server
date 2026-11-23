using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public class EnglishAnimalRecord
{
    public EnglishAnimal English { get; set; } = new();
    public AnimalTexts? German { get; set; }
    public AnimalTexts? French { get; set; }
}
