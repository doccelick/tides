using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace Tides.Api.WaterLevels;

public static class WaterLevelEndpoints
{
    public static IEndpointRouteBuilder MapWaterLevelEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stations/{code}/water-level", GetWaterLevelAsync)
            .WithName("GetWaterLevel")
            .WithTags("Water levels")
            .WithSummary("Returns the latest observed water level from the last 30 minutes for a specified station.")
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return routes;
    }

    private static async Task<Results<Ok<WaterLevelResponse>, ProblemHttpResult, StatusCodeHttpResult>> GetWaterLevelAsync(
        string code,
        WaterLevelService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var snapshot = await service.GetWaterLevelAsync(code, cancellationToken);

        if (snapshot is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Station not found.");
        }

        if (snapshot.Response is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "No water level for this station.");
        }

        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5),
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
