namespace Tides.Api.Imports;

public sealed record ImportsResponse(
    IReadOnlyList<ImportResponse> Imports
);
