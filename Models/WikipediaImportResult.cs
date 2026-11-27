namespace AnimalDatabase.Api.Models;

public class WikipediaImportResult
{
    public string? CommonName { get; set; }
    public string? ScientificName { get; set; }
    public string? Family { get; set; }
    public string? TaxonomicClass { get; set; }
    public decimal? LengthMmMin { get; set; }
    public decimal? LengthMmMax { get; set; }
    public decimal? HeightMmMin { get; set; }
    public decimal? HeightMmMax { get; set; }
    public decimal? WeightGramsMin { get; set; }
    public decimal? WeightGramsMax { get; set; }
    public decimal? LifespanMonthsMin { get; set; }
    public decimal? LifespanMonthsMax { get; set; }
    public string? IucnStatus { get; set; }
    public string? Habitat { get; set; }
    public string? RangeRegion { get; set; }
    public string? Profile { get; set; }
    public string? ImageUrl { get; set; }
    public string? Attribution { get; set; }
    public string? SourcePageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public List<string> Warnings { get; set; } = new();
}