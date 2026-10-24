using Microsoft.AspNetCore.Http.HttpResults;
using Tides.Api.Data;

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

        return routes;
    }

    private static async Task<Results<Created<LocationResponse>, ProblemHttpResult>> CreateLocationAsync(
        CreateLocationRequest request,
        TidesDbContext database,
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

        var location = new Location
        {
            Name = request.Name,
            Latitude = request.Latitude!.Value,
            Longitude = request.Longitude!.Value
        };

        database.Locations.Add(location);
        await database.SaveChangesAsync(cancellationToken);

        var response = new LocationResponse(location.Id, location.Name, location.Latitude, location.Longitude);
        return TypedResults.Created((string?)null, response);
    }
}
