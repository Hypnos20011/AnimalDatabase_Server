using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class CreateAnimalRequest : IValidatableObject
{
    [Required] [MaxLength(120)] public string CommonName { get; set; } = string.Empty;

    [MaxLength(150)] public string? ScientificName { get; set; }

    [MaxLength(120)] public string? Family { get; set; }

    [MaxLength(40)] public string? TaxonomicClass { get; set; }

    public bool IsDomesticated { get; set; }

    [Range(0, 999999.9)] public decimal? SizeLengthCmMin { get; set; }

    [Range(0, 999999.9)] public decimal? SizeLengthCmMax { get; set; }

    [Range(0, 999999.9)] public decimal? SizeHeightCmMin { get; set; }

    [Range(0, 999999.9)] public decimal? SizeHeightCmMax { get; set; }

    [Range(0, 9999999.99)] public decimal? WeightKgMin { get; set; }

    [Range(0, 9999999.99)] public decimal? WeightKgMax { get; set; }

    [Range(0, int.MaxValue)] public int? LifespanYearsMin { get; set; }

    [Range(0, int.MaxValue)] public int? LifespanYearsMax { get; set; }

    [MaxLength(40)]
    [AllowedValues("least concern", "near threatened", "vulnerable", "endangered", "critically endangered",
        "extinct in the wild", "extinct", "data deficient", null)]
    public string? IucnStatus { get; set; }

    [MaxLength(255)] public string? Habitat { get; set; }

    [AllowedValues("forest", "grassland", "desert", "mountains", "wetland", "freshwater", "ocean", "deep sea",
        "underground", null)]
    [MaxLength(30)]
    public string? HabitatType { get; set; }

    [MaxLength(255)] public string? RangeRegion { get; set; }

    [MaxLength(20000)] public string? Profile { get; set; }

    [MaxLength(500)] [Url] public string? SourceUrl { get; set; }

    public List<CreateAnimalImageRequest> Images { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        (string Label, decimal? Min, decimal? Max, string Member)[] ranges =
        [
            ("Length", SizeLengthCmMin, SizeLengthCmMax, nameof(SizeLengthCmMin)),
            ("Height", SizeHeightCmMin, SizeHeightCmMax, nameof(SizeHeightCmMin)),
            ("Weight", WeightKgMin, WeightKgMax, nameof(WeightKgMin)),
            ("Lifespan", LifespanYearsMin, LifespanYearsMax, nameof(LifespanYearsMin))
        ];

        foreach (var range in ranges)
        {
            if (range.Min > range.Max)
            {
                yield return new ValidationResult(
                    $"{range.Label}: the minimum can't be larger than the maximum.", [range.Member]);
            }
        }
    }
}