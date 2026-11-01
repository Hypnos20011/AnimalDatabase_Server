namespace AnimalDatabase.Api.Models;

public class AnimalStats
{
    public int TotalAnimals { get; set; }
    public int DomesticatedAnimals { get; set; }
    public Dictionary<string, int> ByClass { get; set; } = new();
    public Dictionary<string, int> ByIucnStatus { get; set; } = new();
    public Dictionary<string, int> ByHabitatType { get; set; } = new();
}