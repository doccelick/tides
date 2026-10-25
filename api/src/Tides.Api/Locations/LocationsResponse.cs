namespace Tides.Api.Locations;

public sealed record LocationsResponse(
    IReadOnlyList<LocationResponse> Locations
);
