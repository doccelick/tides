namespace Tides.Api.Tides;

public sealed record TidesResponse(
    string StationCode,
    string TimeZone,
    string Unit,
    string Reference,
    IReadOnlyList<TideResponse> Tides
);
