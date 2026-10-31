using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Tides.Api.Http;
using Tides.Api.Locations;

namespace Tides.Api.Samples;

public static class SampleEndpoints
{
    private const int DefaultDays = 2;
    private const int MaxDays = 31;
    private const int DefaultIntervalMinutes = 10;

    public static IEndpointRouteBuilder MapSampleEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/locations/{id:int:min(1)}/samples", GetLocationSamplesAsync)
            .WithName("GetLocationSamples")
            .WithTags("Samples")
            .WithSummary("Returns predicted water levels every 10 or 60 minutes for a range of UTC days for a specified location, read from its imports. Defaults to today and tomorrow every 10 minutes.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<Results<Ok<SamplesResponse>, ValidationProblem, ProblemHttpResult, StatusCodeHttpResult>> GetLocationSamplesAsync(
        int id,
        DateOnly? from,
        [Range(1, MaxDays)] int? days,
        [AllowedValues(10, 60)] int? interval,
        LocationService locationService,
        SampleService sampleService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (DayRangeValidation.Validate(from, days ?? DefaultDays) is { } invalidRange)
        {
            return invalidRange;
        }

        var location = await locationService.GetLocationAsync(id, cancellationToken);

        if (location is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var snapshot = await sampleService.GetSamplesAsync(
            location,
            from,
            days ?? DefaultDays,
            interval ?? DefaultIntervalMinutes,
            cancellationToken);

        if (snapshot is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "No imported water levels for this range.");
        }

        // Adding or deleting an import changes the response, so clients revalidate with the ETag before reusing it.
        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { NoCache = true };
        context.Response.Headers.ETag = snapshot.ETag;

        if (ETags.Matches(context.Request, snapshot.ETag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(snapshot.Response);
    }
}
