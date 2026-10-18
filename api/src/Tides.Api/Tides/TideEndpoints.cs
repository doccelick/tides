using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace Tides.Api.Tides;

public static class TideEndpoints
{
    public static IEndpointRouteBuilder MapTideEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations/{code}/tides", GetTidesAsync)
            .WithName("GetTides")
            .WithTags("Tides")
            .WithSummary("Returns high and low tides for today and tomorrow (UTC) for a specified station.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<TidesResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetTidesAsync(
        string code,
        TideService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var snapshot = await service.GetTidesAsync(code, cancellationToken);

        if (snapshot is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Station not found.");
        }

        if (snapshot.Response.Tides.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "No tide data for this station.");
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
