using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Http;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.Samples;

public sealed class SampleService(
    TidesDbContext database,
    ImportService importService,
    TimeProvider timeProvider)
{
    // Returns null when the location's imports don't cover the whole range.
    public async Task<SamplesSnapshot?> GetSamplesAsync(
        Location location,
        DateOnly? firstDay,
        int days,
        int intervalMinutes,
        CancellationToken cancellationToken = default)
    {
        var day = firstDay ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(days);

        if (!await importService.CoversRangeAsync(location.Id, from, to, cancellationToken))
        {
            return null;
        }

        var predictions = await database.TideEntries
            .AsNoTracking()
            .Where(e => e.LocationId == location.Id
                && e.Type == TideEntryType.Prediction
                && from <= e.Time
                && e.Time < to)
            .OrderBy(e => e.Time)
            .Select(e => new SampleResponse(e.Time, e.LevelCm))
            .ToListAsync(cancellationToken);

        // Imports store a prediction every 10 minutes from midnight UTC, so a longer interval
        // keeps the stored values that fall on it, such as every sixth one for 60 minutes.
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        var samples = predictions
            .Where(p => (p.Time - from).Ticks % interval.Ticks == 0)
            .ToList();

        var response = new SamplesResponse(location.Id, "UTC", "cm", "CD", intervalMinutes, samples);

        return new SamplesSnapshot(response, ETags.Compute(response));
    }
}
