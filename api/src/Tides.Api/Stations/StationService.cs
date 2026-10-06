using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
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

            return new StationsSnapshot(response, ComputeETag(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    private static string ComputeETag(StationsResponse response)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(response));
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
