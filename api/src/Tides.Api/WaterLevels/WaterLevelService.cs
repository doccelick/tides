using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

namespace Tides.Api.WaterLevels;

public sealed class WaterLevelService(
    HybridCache cache,
    StationService stations,
    KartverketClient kartverket,
    TimeProvider timeProvider)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
    };

    // Kartverket returns no observations when fromtime is less than about an hour ago.
    private static readonly TimeSpan RequestRange = TimeSpan.FromHours(2);

    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    public async ValueTask<WaterLevelSnapshot?> GetWaterLevelAsync(string code, CancellationToken cancellationToken = default)
    {
        var stationsSnapshot = await stations.GetStationsAsync(cancellationToken);
        var station = stationsSnapshot.Response.Stations.FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));

        if (station is null)
        {
            return null;
        }

        return await cache.GetOrCreateAsync($"water-level:{station.Code}", async ct =>
        {
            var now = timeProvider.GetUtcNow();
            var observations = await kartverket.GetObservationsAsync(station.Latitude, station.Longitude, now - RequestRange, now, ct);
            var newest = observations.LastOrDefault();

            var response = newest is null || now - newest.Time > MaxAge
                ? null
                : new WaterLevelResponse(station.Code, "UTC", "cm", "CD", newest.Time, newest.LevelCm);

            return new WaterLevelSnapshot(response, ComputeETag(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    private static string ComputeETag(WaterLevelResponse? response)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(response));
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
