using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.Tides;

public sealed class TideService(
    HybridCache cache,
    KartverketClient kartverket,
    TidesDbContext database,
    TimeProvider timeProvider)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(24),
    };

    public async ValueTask<TidesSnapshot> GetTidesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var (from, to) = GetTodayAndTomorrow();

        var cacheKey = string.Create(CultureInfo.InvariantCulture, $"tides:{latitude}:{longitude}:{from:yyyy-MM-dd}");

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            var tides = await kartverket.GetTidesAsync(latitude, longitude, from, to, ct);

            return CreateSnapshot([.. tides.Select(t => new TideResponse(t.Type, t.Time, t.LevelCm))]);
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    public async ValueTask<TidesSnapshot> GetTidesAsync(Location location, CancellationToken cancellationToken = default)
    {
        var (from, to) = GetTodayAndTomorrow();

        if (!await IsCoveredByImportsAsync(location.Id, from, to, cancellationToken))
        {
            return await GetTidesAsync(location.Latitude, location.Longitude, cancellationToken);
        }

        return CreateSnapshot(await GetStoredTidesAsync(location.Id, from, to, cancellationToken));
    }

    private (DateTimeOffset From, DateTimeOffset To) GetTodayAndTomorrow()
    {
        var from = new DateTimeOffset(timeProvider.GetUtcNow().Date, TimeSpan.Zero);

        return (from, from.AddDays(2));
    }

    private async Task<bool> IsCoveredByImportsAsync(
        int locationId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var imports = await database.Imports
            .AsNoTracking()
            .Where(i => i.LocationId == locationId && i.From < to && from < i.To)
            .OrderBy(i => i.From)
            .Select(i => new { i.From, i.To })
            .ToListAsync(cancellationToken);

        var coveredUntil = from;

        // Imports for a location never overlap, so the range is covered only if
        // each import starts where the previous one ended.
        foreach (var import in imports)
        {
            if (import.From > coveredUntil)
            {
                return false;
            }

            coveredUntil = import.To;
        }

        return coveredUntil >= to;
    }

    private async Task<IReadOnlyList<TideResponse>> GetStoredTidesAsync(
        int locationId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var entries = await database.TideEntries
            .AsNoTracking()
            .Where(e => e.LocationId == locationId
                && e.Type != TideEntryType.Prediction
                && from <= e.Time
                && e.Time < to)
            .OrderBy(e => e.Time)
            .Select(e => new { e.Type, e.Time, e.LevelCm })
            .ToListAsync(cancellationToken);

        return [.. entries.Select(e => new TideResponse(GetTideType(e.Type), e.Time, e.LevelCm))];
    }

    private static TideType GetTideType(TideEntryType entryType) => entryType switch
    {
        TideEntryType.High => TideType.High,
        TideEntryType.Low => TideType.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null),
    };

    private static TidesSnapshot CreateSnapshot(IReadOnlyList<TideResponse> tides)
    {
        var response = new TidesResponse("UTC", "cm", "CD", tides);

        return new TidesSnapshot(response, ComputeETag(response));
    }

    private static string ComputeETag(TidesResponse response)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(response));
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
