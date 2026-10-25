using System.ComponentModel.DataAnnotations;

namespace Tides.Api.Imports;

public sealed record CreateImportRequest(
    [Required] DateTimeOffset? From,
    [Required] DateTimeOffset? To
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From == null || To == null)
        {
            yield break;
        }

        if (To <= From)
        {
            yield return new ValidationResult("To must be after From.", [nameof(To)]);
        }

        var span = To - From;
        if (span > TimeSpan.FromDays(Import.MaxDays))
        {
            yield return new ValidationResult($"The range can be at most {Import.MaxDays} days.", [nameof(To)]);
        }
    }
}
