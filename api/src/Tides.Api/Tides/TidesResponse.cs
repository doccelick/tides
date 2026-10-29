namespace Tides.Api.Tides;

public sealed record TidesResponse(
    string TimeZone,
    string Unit,
    string Reference,
    IReadOnlyList<TideResponse> Tides
);
