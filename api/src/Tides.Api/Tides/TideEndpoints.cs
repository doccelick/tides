using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Tides.Api.Locations;
using Tides.Api.Stations;

namespace Tides.Api.Tides;

public static class TideEndpoints
{
    private const int DefaultDays = 2;
    private const int MaxDays = 31;

    public static IEndpointRouteBuilder MapTideEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations/{code}/tides", GetStationTidesAsync)
            .WithName("GetStationTides")
            .WithTags("Tides")
            .WithSummary("Returns high and low tides for a range of UTC days for a specified station. Defaults to today and tomorrow.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        routes.MapGet("/locations/{id:int:min(1)}/tides", GetLocationTidesAsync)
            .WithName("GetLocationTides")
            .WithTags("Tides")
            .WithSummary("Returns high and low tides for a range of UTC days for a specified location. Defaults to today and tomorrow.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetStationTidesAsync(
        string code,
        DateOnly? from,
        [Range(1, MaxDays)] int? days,
        StationService stationService,
        TideService tideService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var station = await stationService.GetStationAsync(code, cancellationToken);

        if (station is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Station not found.");
        }

        var snapshot = await tideService.GetTidesAsync(
            station.Latitude,
            station.Longitude,
            from,
            days ?? DefaultDays,
            cancellationToken);

        var cacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };

        return ToResult(snapshot, context, "No tide data for this station.", cacheControl);
    }

    private static async Task<Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetLocationTidesAsync(
        int id,
        DateOnly? from,
        [Range(1, MaxDays)] int? days,
        LocationService locationService,
        TideService tideService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetLocationAsync(id, cancellationToken);

        if (location is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var snapshot = await tideService.GetTidesAsync(location, from, days ?? DefaultDays, cancellationToken);

        // Adding or deleting an import changes the response, so clients revalidate with the ETag before reusing it.
        var cacheControl = new CacheControlHeaderValue { NoCache = true };

        return ToResult(snapshot, context, "No tide data for this location.", cacheControl);
    }

    private static Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult> ToResult(
        TidesSnapshot snapshot,
        HttpContext context,
        string noDataTitle,
        CacheControlHeaderValue cacheControl)
    {
        if (snapshot.Response.Tides.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: noDataTitle);
        }

        context.Response.GetTypedHeaders().CacheControl = cacheControl;
        context.Response.Headers.ETag = snapshot.ETag;

        if (MatchesETag(context.Request, snapshot.ETag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(snapshot.Response);
    }

    private static bool MatchesETag(HttpRequest request, string eTag)
    {
        var current = new EntityTagHeaderValue(eTag);

        return request.GetTypedHeaders().IfNoneMatch.Any(tag =>
            tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(current, useStrongComparison: false));
    }
}
