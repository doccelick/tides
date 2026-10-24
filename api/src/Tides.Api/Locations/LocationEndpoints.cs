using Microsoft.AspNetCore.Http.HttpResults;

namespace Tides.Api.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/locations", CreateLocationAsync)
            .WithName("CreateLocation")
            .WithTags("Locations")
            .WithSummary("Creates a location.")
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesValidationProblem();

        routes.MapGet("/locations", GetLocationsAsync)
            .WithName("GetLocations")
            .WithTags("Locations")
            .WithSummary("Gets all stored locations.");

        return routes;
    }

    private static async Task<Results<Created<LocationResponse>, ProblemHttpResult>> CreateLocationAsync(
        CreateLocationRequest request,
        LocationService service,
        CancellationToken cancellationToken)
    {
        var isNearCoast = await service.IsNearCoastAsync(
            request.Latitude!.Value,
            request.Longitude!.Value,
            cancellationToken);

        if (!isNearCoast)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Too far from the coast.");
        }

        var response = await service.CreateLocationAsync(
            request.Name,
            request.Latitude!.Value,
            request.Longitude!.Value,
            cancellationToken);

        return TypedResults.Created((string?)null, response);
    }

    private static async Task<Ok<LocationsResponse>> GetLocationsAsync(
        LocationService service,
        CancellationToken cancellationToken)
    {
        var response = await service.GetLocationsAsync(cancellationToken);
        return TypedResults.Ok(response);
    }
}
