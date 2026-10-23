using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.Imports;

public sealed class ImportService(TidesDbContext database, KartverketClient kartverket, TimeProvider timeProvider)
{
    public async Task<Import?> ImportAsync(
        Location location,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var overlaps = await database.Imports.AnyAsync(
            i => i.LocationId == location.Id && i.From < to && from < i.To,
            cancellationToken);

        if (overlaps)
        {
            return null;
        }

        var tides = await kartverket.GetTidesAsync(
            location.Latitude,
            location.Longitude,
            from,
            to,
            cancellationToken);

        var predictions = await kartverket.GetPredictionsAsync(
            location.Latitude,
            location.Longitude,
            from,
            to,
            cancellationToken);

        var import = new Import
        {
            LocationId = location.Id,
            From = from,
            To = to,
            ImportedAt = timeProvider.GetUtcNow()
        };

        foreach (var tide in tides.Where(t => import.Covers(t.Time)))
        {
            import.Entries.Add(new TideEntry
            {
                LocationId = location.Id,
                Time = tide.Time,
                Type = GetTideEntryType(tide.Type),
                LevelCm = tide.LevelCm
            });
        }

        foreach (var prediction in predictions.Where(p => import.Covers(p.Time)))
        {
            import.Entries.Add(new TideEntry
            {
                LocationId = location.Id,
                Time = prediction.Time,
                Type = TideEntryType.Prediction,
                LevelCm = prediction.LevelCm
            });
        }

        database.Imports.Add(import);
        await database.SaveChangesAsync(cancellationToken);
        return import;
    }

    private static TideEntryType GetTideEntryType(TideType tideType) => tideType switch
    {
        TideType.High => TideEntryType.High,
        TideType.Low => TideEntryType.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(tideType), tideType, null),
    };
}
