namespace Tides.Api.Stations;

public sealed record StationsResponse(
    IReadOnlyList<StationResponse> Stations
);
