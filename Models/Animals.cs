namespace AnimalDatabase.Api.Models;

public class Animal
{
    public int Id { get; set; }

    public string CommonName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string? Family { get; set; }

    public string? TaxonomicClass { get; set; }
    public bool IsDomesticated { get; set; }

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
    public string? HabitatType { get; set; }
    public string? RangeRegion { get; set; }
    public string? Profile { get; set; }
    public string? SourceUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}