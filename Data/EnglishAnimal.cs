using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public class EnglishAnimal
{
    public int Id { get; set; }
    public string? CommonName { get; set; }
    public string? Habitat { get; set; }
    public string? RangeRegion { get; set; }
    public string? Profile { get; set; }

    public AnimalTexts ToTexts() => new()
    {
        CommonName = CommonName,
        Habitat = Habitat,
        RangeRegion = RangeRegion,
        Profile = Profile
    };
}
