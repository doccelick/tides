namespace Tides.Api.Stations;

public sealed record NearbyStation(
    StationResponse Station,
    double DistanceKm
);
