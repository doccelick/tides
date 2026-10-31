using Microsoft.AspNetCore.Http.HttpResults;
using Tides.Api.Auth;

namespace Tides.Api.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/locations", CreateLocationAsync)
            .WithName("CreateLocation")
            .WithTags("Locations")
            .WithSummary("Creates a location.")
            .RequireAuthorization(AuthPolicies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesValidationProblem();

        routes.MapGet("/locations", GetLocationsAsync)
            .WithName("GetLocations")
            .WithTags("Locations")
            .WithSummary("Gets all stored locations.");

        routes.MapDelete("/locations/{id:int:min(1)}", DeleteLocationAsync)
            .WithName("DeleteLocation")
            .WithTags("Locations")
            .WithSummary("Deletes a location and its imported tide data.")
            .RequireAuthorization(AuthPolicies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

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

    private static async Task<Results<NoContent, NotFound>> DeleteLocationAsync(
        int id,
        LocationService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteLocationAsync(id, cancellationToken);

        if (!deleted)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
