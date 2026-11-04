using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tides.Api.Data;

namespace Tides.Api.Imports;

// Writes tide entries with SqlBulkCopy, which is much faster than SaveChanges for thousands of rows.
public sealed class TideEntryWriter(TidesDbContext database)
{
    // Writes in the context's current transaction, so the caller decides when the entries are committed.
    // CheckConstraints keeps the foreign key to Imports trusted, and the unique index on entries
    // still rejects duplicates with SQL Server error 2601.
    public async Task WriteAsync(IEnumerable<TideEntry> entries, CancellationToken cancellationToken = default)
    {
        var transaction = database.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Tide entries must be written inside a transaction.");

        using var table = new DataTable();
        table.Columns.Add(nameof(TideEntry.ImportId), typeof(int));
        table.Columns.Add(nameof(TideEntry.LocationId), typeof(int));
        table.Columns.Add(nameof(TideEntry.Time), typeof(DateTimeOffset));
        table.Columns.Add(nameof(TideEntry.Type), typeof(int));
        table.Columns.Add(nameof(TideEntry.LevelCm), typeof(double));

        foreach (var entry in entries)
        {
            table.Rows.Add(entry.ImportId, entry.LocationId, entry.Time, (int)entry.Type, entry.LevelCm);
        }

        using var bulkCopy = new SqlBulkCopy(
            (SqlConnection)database.Database.GetDbConnection(),
            SqlBulkCopyOptions.CheckConstraints,
            (SqlTransaction)transaction.GetDbTransaction())
        {
            DestinationTableName = nameof(TidesDbContext.TideEntries)
        };

        // Id is left out, so SQL Server assigns it.
        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }
}
