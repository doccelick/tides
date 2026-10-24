using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Kartverket;

namespace Tides.Api.Locations;

public sealed class LocationService(
    KartverketClient kartverket,
    TidesDbContext database,
    TimeProvider timeProvider)
{
    public async Task<bool> IsNearCoastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var from = timeProvider.GetUtcNow();
        var to = from.AddHours(1);

        var predictions = await kartverket.GetPredictionsAsync(
            latitude,
            longitude,
            from,
            to,
            cancellationToken);

        return predictions.Count > 0;
    }

    public async Task<LocationResponse> CreateLocationAsync(
        string name,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var location = new Location
        {
            Name = name,
            Latitude = latitude,
            Longitude = longitude
        };

        database.Locations.Add(location);
        await database.SaveChangesAsync(cancellationToken);

        var response = new LocationResponse(location.Id, location.Name, location.Latitude, location.Longitude);

        return response;
    }

    public async Task<LocationsResponse> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await database.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LocationResponse(l.Id, l.Name, l.Latitude, l.Longitude))
            .ToListAsync(cancellationToken);

        return new LocationsResponse(locations);
    }

    public async Task<bool> DeleteLocationAsync(int id, CancellationToken cancellationToken = default)
    {
        var deletedRows = await database.Locations
            .Where(l => l.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows > 0;
    }

    public async Task<Location?> GetLocationAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await database.Locations.FindAsync([id], cancellationToken);

        return location;
    }
}
