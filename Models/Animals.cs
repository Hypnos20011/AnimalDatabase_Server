namespace AnimalDatabase.Api.Models;

public class Animal
{
    public int Id { get; set; }

    public string CommonName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string? Family { get; set; }

    public string? TaxonomicClass { get; set; }
    public bool IsDomesticated { get; set; }

    public decimal? SizeLengthCmMin { get; set; }
    public decimal? SizeLengthCmMax { get; set; }
    public decimal? SizeHeightCmMin { get; set; }
    public decimal? SizeHeightCmMax { get; set; }

    public decimal? WeightKgMin { get; set; }
    public decimal? WeightKgMax { get; set; }

    public int? LifespanYearsMin { get; set; }
    public int? LifespanYearsMax { get; set; }

    public string? IucnStatus { get; set; }
    public string? Habitat { get; set; }
    public string? RangeRegion { get; set; }
    public string? Profile { get; set; }
    public string? ThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}