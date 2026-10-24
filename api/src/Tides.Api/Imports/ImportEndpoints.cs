using Microsoft.AspNetCore.Http.HttpResults;
using Tides.Api.Locations;

namespace Tides.Api.Imports;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/locations/{id:int:min(1)}/imports", CreateImportAsync)
            .WithName("CreateImport")
            .WithTags("Imports")
            .WithSummary("Creates an import.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesValidationProblem();

        return routes;
    }

    private static async Task<Results<Created<ImportResponse>, ProblemHttpResult>> CreateImportAsync(
        int id,
        CreateImportRequest request,
        ImportService importService,
        LocationService locationService,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetLocationAsync(id, cancellationToken);

        if (location == null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var import = await importService.ImportAsync(location, request.From!.Value, request.To!.Value, cancellationToken);

        if (import == null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Range overlaps existing import.");
        }

        var importResponse = new ImportResponse(
            import.Id,
            import.LocationId,
            import.From,
            import.To,
            import.ImportedAt,
            import.Entries.Count);

        return TypedResults.Created((string?)null, importResponse);
    }
}
