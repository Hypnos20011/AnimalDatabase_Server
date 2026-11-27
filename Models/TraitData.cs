namespace AnimalDatabase.Api.Models;

public class TraitData
{
    public decimal? LengthMmMin { get; set; }
    public decimal? LengthMmMax { get; set; }
    public decimal? HeightMmMin { get; set; }
    public decimal? HeightMmMax { get; set; }
    public decimal? WeightGramsMin { get; set; }
    public decimal? WeightGramsMax { get; set; }
    public decimal? LifespanMonthsMin { get; set; }
    public decimal? LifespanMonthsMax { get; set; }
    public string? Habitat { get; set; }
}