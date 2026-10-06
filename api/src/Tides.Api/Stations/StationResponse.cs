namespace Tides.Api.Stations;

public sealed record StationResponse(
    string Code,
    string Name,
    double Latitude,
    double Longitude
);
