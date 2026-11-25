using System.ComponentModel.DataAnnotations;

namespace AnimalDatabase.Api.Models;

public class CreateAnimalImageRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^(male|female|baby|unspecified)$")]
    public string Kind { get; set; } = string.Empty;

    [Required] [MaxLength(500)] [Url] public string Url { get; set; } = string.Empty;

    [MaxLength(200)] public string? Caption { get; set; }

    [MaxLength(500)] public string? Attribution { get; set; }

    [MaxLength(500)] [Url] public string? SourcePageUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Url) &&
            !(Uri.TryCreate(Url, UriKind.Absolute, out var image) && image.Scheme == Uri.UriSchemeHttps))
        {
            yield return new ValidationResult(
                "Image link: only https:// links are allowed.",
                [nameof(Url)]);
        }

        if (!string.IsNullOrEmpty(SourcePageUrl) &&
            !(Uri.TryCreate(SourcePageUrl, UriKind.Absolute, out var source) && source.Scheme == Uri.UriSchemeHttps))
        {
            yield return new ValidationResult(
                "Image source page: only https:// links are allowed.",
                [nameof(SourcePageUrl)]);
        }
    }
}