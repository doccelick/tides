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
            .ProducesValidationProblem();

        return routes;
    }

    private static async Task<Created<LocationResponse>> CreateLocationAsync(
        CreateLocationRequest request,
        TidesDbContext database,
        CancellationToken cancellationToken)
    {
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
