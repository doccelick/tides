using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Tides.Api.Locations;
using Tides.Api.Stations;

namespace Tides.Api.Tides;

public static class TideEndpoints
{
    public static IEndpointRouteBuilder MapTideEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations/{code}/tides", GetStationTidesAsync)
            .WithName("GetStationTides")
            .WithTags("Tides")
            .WithSummary("Returns high and low tides for today and tomorrow (UTC) for a specified station.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        routes.MapGet("/locations/{id:int:min(1)}/tides", GetLocationTidesAsync)
            .WithName("GetLocationTides")
            .WithTags("Tides")
            .WithSummary("Returns high and low tides for today and tomorrow (UTC) for a specified location.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetStationTidesAsync(
        string code,
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

        var snapshot = await tideService.GetTidesAsync(station.Latitude, station.Longitude, cancellationToken);

        return ToResult(snapshot, context, "No tide data for this station.");
    }

    private static async Task<Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetLocationTidesAsync(
        int id,
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

        var snapshot = await tideService.GetTidesAsync(location, cancellationToken);

        return ToResult(snapshot, context, "No tide data for this location.");
    }

    private static Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult> ToResult(
        TidesSnapshot snapshot,
        HttpContext context,
        string noDataTitle)
    {
        if (snapshot.Response.Tides.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: noDataTitle);
        }

        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };
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
