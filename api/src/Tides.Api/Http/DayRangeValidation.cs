using Microsoft.AspNetCore.Http.HttpResults;

namespace Tides.Api.Http;

public static class DayRangeValidation
{
    // A range of UTC days ends at midnight after its last day. DateTimeOffset can't hold
    // midnight after 9999-12-31, so the last day must be 9999-12-30 or earlier.
    public static ValidationProblem? Validate(DateOnly? from, int days)
    {
        if (from is not { } firstDay || firstDay.DayNumber + days <= DateOnly.MaxValue.DayNumber)
        {
            return null;
        }

        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["from"] = ["The range must end before 9999-12-31."],
        });
    }
}
