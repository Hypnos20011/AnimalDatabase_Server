namespace AnimalDatabase.Api.Models;

public class TraitData
{
    public decimal? SizeLengthCmMin { get; set; }
    public decimal? SizeLengthCmMax { get; set; }
    public decimal? WeightKgMin { get; set; }
    public decimal? WeightKgMax { get; set; }
    public int? LifespanYearsMin { get; set; }
    public int? LifespanYearsMax { get; set; }
    public string? Habitat { get; set; }
}