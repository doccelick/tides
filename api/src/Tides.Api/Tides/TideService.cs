using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Data;
using Tides.Api.Http;
using Tides.Api.Imports;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.Tides;

public sealed class TideService(
    HybridCache cache,
    KartverketClient kartverket,
    TidesDbContext database,
    ImportService importService,
    TimeProvider timeProvider)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(24),
    };

    public async ValueTask<TidesSnapshot> GetTidesAsync(
        double latitude,
        double longitude,
        DateOnly? firstDay,
        int days,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = GetRange(firstDay, days);

        return await GetLiveTidesAsync(latitude, longitude, from, to, cancellationToken);
    }

    public async ValueTask<TidesSnapshot> GetTidesAsync(
        Location location,
        DateOnly? firstDay,
        int days,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = GetRange(firstDay, days);

        if (!await importService.CoversRangeAsync(location.Id, from, to, cancellationToken))
        {
            return await GetLiveTidesAsync(location.Latitude, location.Longitude, from, to, cancellationToken);
        }

        return CreateSnapshot(TideSource.Database, await GetStoredTidesAsync(location.Id, from, to, cancellationToken));
    }

    private (DateTimeOffset From, DateTimeOffset To) GetRange(DateOnly? firstDay, int days)
    {
        var day = firstDay ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return (from, from.AddDays(days));
    }

    private async ValueTask<TidesSnapshot> GetLiveTidesAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var cacheKey = string.Create(CultureInfo.InvariantCulture, $"tides:{latitude}:{longitude}:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}");

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            var tides = await kartverket.GetTidesAsync(latitude, longitude, from, to, ct);

            return CreateSnapshot(TideSource.Kartverket, [.. tides.Select(t => new TideResponse(t.Type, t.Time, t.LevelCm))]);
        }, CacheOptions, cancellationToken: cancellationToken);
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

    private static TidesSnapshot CreateSnapshot(TideSource source, IReadOnlyList<TideResponse> tides)
    {
        var response = new TidesResponse("UTC", "cm", "CD", source, tides);

        return new TidesSnapshot(response, ETags.Compute(response));
    }
}
