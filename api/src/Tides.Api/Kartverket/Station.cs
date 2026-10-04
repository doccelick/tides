namespace Tides.Api.Kartverket;

public sealed record Station(
    string Code,
    string Name,
    double Latitude,
    double Longitude
);
