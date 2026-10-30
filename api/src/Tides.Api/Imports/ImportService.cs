using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.Imports;

public sealed class ImportService(TidesDbContext database, KartverketClient kartverket, TimeProvider timeProvider)
{
    // Creates one import for each part of the range that no stored import covers.
    // Returns an empty list when the whole range is already imported.
    public async Task<IReadOnlyList<Import>> ImportAsync(
        Location location,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var stored = await database.Imports
            .AsNoTracking()
            .Where(i => i.LocationId == location.Id && i.From < to && from < i.To)
            .Select(i => new { i.From, i.To })
            .ToListAsync(cancellationToken);

        var gaps = ImportGaps.Find(from, to, stored.Select(i => (i.From, i.To)));

        if (gaps.Count == 0)
        {
            return [];
        }

        // One fetch spans all gaps, and each import keeps only the entries in its own gap.
        var fetchFrom = gaps[0].From;
        var fetchTo = gaps[^1].To;

        var tides = await kartverket.GetTidesAsync(
            location.Latitude,
            location.Longitude,
            fetchFrom,
            fetchTo,
            cancellationToken);

        var predictions = await kartverket.GetPredictionsAsync(
            location.Latitude,
            location.Longitude,
            fetchFrom,
            fetchTo,
            cancellationToken);

        var importedAt = timeProvider.GetUtcNow();
        var imports = gaps
            .Select(gap => CreateImport(location, gap.From, gap.To, importedAt, tides, predictions))
            .ToList();

        database.Imports.AddRange(imports);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        // A concurrent import can pass the gap check above. SQL Server error 2601 is the
        // unique index on entries rejecting the duplicate rows.
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 })
        {
            return [];
        }

        return imports;
    }

    private static Import CreateImport(
        Location location,
        DateTimeOffset from,
        DateTimeOffset to,
        DateTimeOffset importedAt,
        IReadOnlyList<Tide> tides,
        IReadOnlyList<Prediction> predictions)
    {
        var import = new Import
        {
            LocationId = location.Id,
            From = from,
            To = to,
            ImportedAt = importedAt
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

        return import;
    }

    public async Task<ImportsResponse> GetImportsAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var imports = await database.Imports
            .AsNoTracking()
            .OrderBy(i => i.From)
            .Where(i => i.LocationId == locationId)
            .Select(i => new ImportResponse(i.Id, i.LocationId, i.From, i.To, i.ImportedAt, i.Entries.Count))
            .ToListAsync(cancellationToken);

        return new ImportsResponse(imports);
    }

    public async Task<bool> DeleteImportAsync(int importId, CancellationToken cancellationToken = default)
    {
        var deletedRows = await database.Imports
            .Where(i => i.Id == importId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows > 0;
    }

    private static TideEntryType GetTideEntryType(TideType tideType) => tideType switch
    {
        TideType.High => TideEntryType.High,
        TideType.Low => TideEntryType.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(tideType), tideType, null),
    };
}
