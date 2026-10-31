namespace Tides.Api.Tides;

public sealed record TidesResponse(
    string TimeZone,
    string Unit,
    string Reference,
    TideSource Source,
    IReadOnlyList<TideResponse> Tides
);
