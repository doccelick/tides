using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Http;
using Tides.Api.Kartverket;

namespace Tides.Api.Stations;

public sealed class StationService(HybridCache cache, KartverketClient kartverket)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(24),
    };

    public async ValueTask<StationsSnapshot> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync("stations", async ct =>
        {
            var stations = await kartverket.GetStationsAsync(ct);
            var response = new StationsResponse(
                [.. stations.Select(s => new StationResponse(s.Code, s.Name, s.Latitude, s.Longitude))]);

            return new StationsSnapshot(response, ETags.Compute(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    public async ValueTask<StationResponse?> GetStationAsync(string code, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetStationsAsync(cancellationToken);

        return snapshot.Response.Stations.FirstOrDefault(
            s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public async ValueTask<NearbyStation?> GetNearestStationAsync(
        double latitude,
        double longitude,
        double maxDistanceKm,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetStationsAsync(cancellationToken);

        var nearest = snapshot.Response.Stations
            .Select(s => new NearbyStation(s, GeoDistance.GetKilometers(latitude, longitude, s.Latitude, s.Longitude)))
            .MinBy(s => s.DistanceKm);

        return nearest is not null && nearest.DistanceKm <= maxDistanceKm ? nearest : null;
    }
}
