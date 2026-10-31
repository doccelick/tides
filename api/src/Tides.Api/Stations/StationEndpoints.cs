using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Tides.Api.Http;

namespace Tides.Api.Stations;

public static class StationEndpoints
{
    public static IEndpointRouteBuilder MapStationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations", GetStationsAsync)
            .WithName("GetStations")
            .WithTags("Stations")
            .WithSummary("Lists Kartverket's permanent tide stations.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<StationsResponse>, StatusCodeHttpResult>> GetStationsAsync(
        StationService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var snapshot = await service.GetStationsAsync(cancellationToken);

        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };
        context.Response.Headers.ETag = snapshot.ETag;

        if (ETags.Matches(context.Request, snapshot.ETag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(snapshot.Response);
    }
}
