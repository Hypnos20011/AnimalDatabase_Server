namespace AnimalDatabase.Api.Models;

public class WikipediaImportResult
{
    public string? CommonName { get; set; }
    public string? ScientificName { get; set; }
    public string? Family { get; set; }
    public string? TaxonomicClass { get; set; }
    public int? SizeLengthCmMin { get; set; }
    public int? SizeLengthCmMax { get; set; }
    public decimal? WeightKgMin { get; set; }
    public decimal? WeightKgMax { get; set; }
    public int? LifespanYearsMin { get; set; }
    public int? LifespanYearsMax { get; set; }
    public string? IucnStatus { get; set; }
    public string? Profile { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
}
