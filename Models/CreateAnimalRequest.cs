using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class CreateAnimalRequest : IValidatableObject
{
    [Required] [MaxLength(120)] public string CommonName { get; set; } = string.Empty;

    [MaxLength(150)] public string? ScientificName { get; set; }

    [MaxLength(120)] public string? Family { get; set; }

    [MaxLength(40)] public string? TaxonomicClass { get; set; }

    public bool IsDomesticated { get; set; }

    [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true)] public decimal? LengthMmMin { get; set; }

    [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true)] public decimal? LengthMmMax { get; set; }

    [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true)] public decimal? HeightMmMin { get; set; }

    [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true)] public decimal? HeightMmMax { get; set; }

    [Range(typeof(decimal), "0", "9999999999.999", ParseLimitsInInvariantCulture = true)] public decimal? WeightGramsMin { get; set; }

    [Range(typeof(decimal), "0", "9999999999.999", ParseLimitsInInvariantCulture = true)] public decimal? WeightGramsMax { get; set; }

    [Range(typeof(decimal), "0", "99999.9", ParseLimitsInInvariantCulture = true)] public decimal? LifespanMonthsMin { get; set; }

    [Range(typeof(decimal), "0", "99999.9", ParseLimitsInInvariantCulture = true)] public decimal? LifespanMonthsMax { get; set; }

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

    // https only, matching the form in the frontend; the check lives in Validate below.
    [MaxLength(500)] [Url] public string? SourceUrl { get; set; }

    public List<CreateAnimalImageRequest> Images { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        (string Label, decimal? Min, decimal? Max, string Member)[] ranges =
        [
            ("Length", LengthMmMin, LengthMmMax, nameof(LengthMmMin)),
            ("Height", HeightMmMin, HeightMmMax, nameof(HeightMmMin)),
            ("Weight", WeightGramsMin, WeightGramsMax, nameof(WeightGramsMin)),
            ("Lifespan", LifespanMonthsMin, LifespanMonthsMax, nameof(LifespanMonthsMin))
        ];

        if (!string.IsNullOrEmpty(SourceUrl) &&
            !(Uri.TryCreate(SourceUrl, UriKind.Absolute, out var source) && source.Scheme == Uri.UriSchemeHttps))
        {
            yield return new ValidationResult("Source link: only https:// links are allowed.", [nameof(SourceUrl)]);
        }

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