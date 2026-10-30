using Microsoft.AspNetCore.Http.HttpResults;
using Tides.Api.Locations;

namespace Tides.Api.Imports;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/locations/{locationId:int:min(1)}/imports", CreateImportAsync)
            .WithName("CreateImport")
            .WithTags("Imports")
            .WithSummary("Imports the parts of a range that are not imported yet.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesValidationProblem();

        routes.MapGet("/locations/{locationId:int:min(1)}/imports", GetImportsAsync)
            .WithName("GetImports")
            .WithTags("Imports")
            .WithSummary("Lists the imports for a location.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        routes.MapDelete("/imports/{importId:int:min(1)}", DeleteImportAsync)
            .WithName("DeleteImport")
            .WithTags("Imports")
            .WithSummary("Deletes an import and its tide data");

        return routes;
    }

    private static async Task<Results<Created<ImportsResponse>, ProblemHttpResult>> CreateImportAsync(
        int locationId,
        CreateImportRequest request,
        ImportService importService,
        LocationService locationService,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetLocationAsync(locationId, cancellationToken);

        if (location == null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var imports = await importService.ImportAsync(location, request.From!.Value, request.To!.Value, cancellationToken);

        if (imports.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Range already imported.");
        }

        var importsResponse = new ImportsResponse([
            .. imports.Select(i => new ImportResponse(i.Id, i.LocationId, i.From, i.To, i.ImportedAt, i.Entries.Count))
        ]);

        return TypedResults.Created((string?)null, importsResponse);
    }

    private static async Task<Results<Ok<ImportsResponse>, ProblemHttpResult>> GetImportsAsync(
        int locationId,
        LocationService locationService,
        ImportService importService,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetLocationAsync(locationId, cancellationToken);

        if (location == null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Location not found.");
        }

        var imports = await importService.GetImportsAsync(locationId, cancellationToken);
        return TypedResults.Ok(imports);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteImportAsync(
        int importId,
        ImportService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteImportAsync(importId, cancellationToken);

        if (!deleted)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
