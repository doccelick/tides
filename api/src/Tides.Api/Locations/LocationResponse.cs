namespace Tides.Api.Locations;

public sealed record LocationResponse(
    int Id,
    string Name,
    double Latitude,
    double Longitude
);
