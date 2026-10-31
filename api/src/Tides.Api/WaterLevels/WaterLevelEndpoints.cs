using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Tides.Api.Http;
using Tides.Api.Locations;
using Tides.Api.Stations;

namespace Tides.Api.WaterLevels;

public static class WaterLevelEndpoints
{
    private const double MaxStationDistanceKm = 50;
    private const string NoEstimateTitle = "No water level estimate for this location.";

    public static IEndpointRouteBuilder MapWaterLevelEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations/{code}/water-level", GetWaterLevelAsync)
            .WithName("GetWaterLevel")
            .WithTags("Water levels")
            .WithSummary("Returns the latest observed water level from the last 30 minutes for a specified station.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        routes.MapGet("/locations/{id:int:min(1)}/water-level", GetLocationWaterLevelAsync)
            .WithName("GetLocationWaterLevel")
            .WithTags("Water levels")
            .WithSummary("Returns an estimated water level for a specified location, from the weather effect at the nearest station within 50 km.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<WaterLevelResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetWaterLevelAsync(
        string code,
        StationService stationService,
        WaterLevelService waterLevelService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var station = await stationService.GetStationAsync(code, cancellationToken);

        if (station is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Station not found.");
        }

        var snapshot = await waterLevelService.GetWaterLevelAsync(station, cancellationToken);

        return ToResult(snapshot.Response, snapshot.ETag, context, "No water level for this station.");
    }

    private static async Task<Results<Ok<EstimatedWaterLevelResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetLocationWaterLevelAsync(
        int id,
        LocationService locationService,
        StationService stationService,
        WaterLevelService waterLevelService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetLocationAsync(id, cancellationToken);

        if (location is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var nearbyStation = await stationService.GetNearestStationAsync(
            location.Latitude,
            location.Longitude,
            MaxStationDistanceKm,
            cancellationToken);

        if (nearbyStation is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: NoEstimateTitle);
        }

        var snapshot = await waterLevelService.GetEstimatedWaterLevelAsync(location, nearbyStation, cancellationToken);

        return ToResult(snapshot.Response, snapshot.ETag, context, NoEstimateTitle);
    }

    private static Results<Ok<T>, ProblemHttpResult, StatusCodeHttpResult> ToResult<T>(
        T? response,
        string eTag,
        HttpContext context,
        string noDataTitle)
        where T : class
    {
        if (response is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: noDataTitle);
        }

        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5),
        };
        context.Response.Headers.ETag = eTag;

        if (ETags.Matches(context.Request, eTag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(response);
    }
}
