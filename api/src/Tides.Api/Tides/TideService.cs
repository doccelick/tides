using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

namespace Tides.Api.Tides;

public sealed class TideService(
    HybridCache cache,
    StationService stations,
    KartverketClient kartverket,
    TimeProvider timeProvider)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(24),
    };

    public async ValueTask<TidesSnapshot?> GetTidesAsync(string code, CancellationToken cancellationToken = default)
    {
        var stationsSnapshot = await stations.GetStationsAsync(cancellationToken);
        var station = stationsSnapshot.Response.Stations.FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));

        if (station is null)
        {
            return null;
        }

        var today = timeProvider.GetUtcNow().Date;
        var from = new DateTimeOffset(today, TimeSpan.Zero);
        var to = from.AddDays(2);

        var cacheKey = string.Create(CultureInfo.InvariantCulture, $"tides:{station.Code}:{today:yyyy-MM-dd}");

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            var tides = await kartverket.GetTidesAsync(station.Latitude, station.Longitude, from, to, ct);
            var response = new TidesResponse(
                station.Code,
                "UTC",
                "cm",
                "MSL",
                [.. tides.Select(
                    t => new TideResponse(
                        t.Type,
                        t.Time,
                        t.LevelCm
                        ))]);

            return new TidesSnapshot(response, ComputeETag(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    private static string ComputeETag(TidesResponse response)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(response));
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
