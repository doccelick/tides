using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.Imports;

public sealed class ImportService(
    TidesDbContext database,
    KartverketClient kartverket,
    TideEntryWriter entryWriter,
    TimeProvider timeProvider)
{
    // Imports the parts of the location's window that no stored import covers.
    public Task<IReadOnlyList<Import>> ImportWindowAsync(Location location, CancellationToken cancellationToken = default)
    {
        var (from, to) = ImportWindow.Around(timeProvider.GetUtcNow());

        return ImportAsync(location, from, to, cancellationToken);
    }

    // Creates one import for each part of the range that no stored import covers.
    // Returns an empty list when the whole range is already imported.
    public async Task<IReadOnlyList<Import>> ImportAsync(
        Location location,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        // Imports run one at a time. The lock is held from the gap check through the commit,
        // so two imports can't find the same gap.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await AcquireImportLockAsync(cancellationToken);

        var gaps = await FindGapsAsync(location.Id, from, to, cancellationToken);

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
            .Select(gap => new Import { LocationId = location.Id, From = gap.From, To = gap.To, ImportedAt = importedAt })
            .ToList();

        database.Imports.AddRange(imports);
        await database.SaveChangesAsync(cancellationToken);

        // The imports are detached before their entries are added, so a later SaveChanges on this
        // context does not insert the entries a second time.
        foreach (var import in imports)
        {
            database.Entry(import).State = EntityState.Detached;
            import.Entries = CreateEntries(import, tides, predictions);
        }

        try
        {
            await entryWriter.WriteAsync(imports.SelectMany(i => i.Entries), cancellationToken);
        }
        // The unique index on entries is a second guard. SQL Server error 2601 means some of the
        // entries are already stored, so nothing is imported and the transaction rolls back.
        catch (SqlException exception) when (exception.Number == 2601)
        {
            return [];
        }

        await transaction.CommitAsync(cancellationToken);

        return imports;
    }

    private static List<TideEntry> CreateEntries(
        Import import,
        IReadOnlyList<Tide> tides,
        IReadOnlyList<Prediction> predictions)
    {
        var entries = new List<TideEntry>();

        foreach (var tide in tides.Where(t => import.Covers(t.Time)))
        {
            entries.Add(new TideEntry
            {
                ImportId = import.Id,
                LocationId = import.LocationId,
                Time = tide.Time,
                Type = GetTideEntryType(tide.Type),
                LevelCm = tide.LevelCm
            });
        }

        foreach (var prediction in predictions.Where(p => import.Covers(p.Time)))
        {
            entries.Add(new TideEntry
            {
                ImportId = import.Id,
                LocationId = import.LocationId,
                Time = prediction.Time,
                Type = TideEntryType.Prediction,
                LevelCm = prediction.LevelCm
            });
        }

        return entries;
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

    // Returns true when the location's imports together cover all of [from, to).
    public async Task<bool> CoversRangeAsync(
        int locationId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var gaps = await FindGapsAsync(locationId, from, to, cancellationToken);

        return gaps.Count == 0;
    }

    // Waits up to 20 seconds for a running import to finish. The lock belongs to the current
    // transaction, so SQL Server releases it on commit or rollback.
    private async Task AcquireImportLockAsync(CancellationToken cancellationToken)
    {
        await database.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = N'imports', @LockMode = N'Exclusive',
                @LockOwner = N'Transaction', @LockTimeout = 20000;
            IF @result < 0 THROW 50000, N'Timed out waiting for another import to finish.', 1;
            """, cancellationToken);
    }

    private async Task<IReadOnlyList<(DateTimeOffset From, DateTimeOffset To)>> FindGapsAsync(
        int locationId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var stored = await database.Imports
            .AsNoTracking()
            .Where(i => i.LocationId == locationId && i.From < to && from < i.To)
            .Select(i => new { i.From, i.To })
            .ToListAsync(cancellationToken);

        return ImportGaps.Find(from, to, stored.Select(i => (i.From, i.To)));
    }

    private static TideEntryType GetTideEntryType(TideType tideType) => tideType switch
    {
        TideType.High => TideEntryType.High,
        TideType.Low => TideEntryType.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(tideType), tideType, null),
    };
}
