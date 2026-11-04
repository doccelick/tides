# 0005. SqlBulkCopy for tide entries

## Status

Accepted

## Context

An import stores the high and low tides and the predicted level every 10 minutes for the previous, current and next month, which is about 13,600 rows per location. EF Core's `SaveChanges` tracks every entry and sends the rows as batched `INSERT` statements. Measured in the integration tests against SQL Server in Docker, with data fetched from Kartverket, a three-month import took 2.0 to 3.1 s. About 0.9 s of that was the fetch.

`SqlBulkCopy` streams the rows to SQL Server as one bulk load. Ten imports for different locations that ran at the same time showed that a bulk load behaves differently from `INSERT` statements:

- With `CHECK_CONSTRAINTS`, SQL Server checks the foreign key to `Imports` for the whole load by reading `PK_Imports`. The deadlock graphs show each load holding an exclusive lock on its own new import and waiting for a shared lock on the new import of another load. Nine of the ten imports were chosen as deadlock victims.
- Without it, SQL Server skips the check and marks the foreign key as not trusted. That changes the table's metadata, so each load takes a schema modification lock on `TideEntries`, and concurrent loads deadlock on those locks. A load that is already running can also fail with "Insert bulk failed due to a schema change of the target table". The foreign key stays untrusted afterwards.

## Decision

- `TideEntryWriter` writes the entries with `SqlBulkCopy`, in the same transaction that EF Core saves the imports in.
- The load uses `SqlBulkCopyOptions.CheckConstraints`, so the foreign key stays trusted.
- Imports run one at a time. `ImportService` opens its transaction first and takes an exclusive application lock with `sp_getapplock`. The transaction owns the lock and holds it from the gap check through the commit.

## Consequences

- A three-month import takes 1.0 to 1.1 s. The database part dropped from about 1.1 to 2.1 s to about 0.1 to 0.2 s, so an import now mostly waits for Kartverket.
- Imports never run in parallel. An import that starts while another import is running waits for it to finish. If the other import is still running after 20 s, the waiting import stops with an error and imports nothing. Normal imports take about 1 s, so the wait only runs out when the other import is slow, for example when Kartverket is slow to answer, because the lock is held during the fetch.
- Two imports can no longer find the same gap, so the unique index on entries is only a second guard.
- A bulk load of 13,249 rows did not escalate to a table lock, so an import does not lock the whole `TideEntries` table.
- Deleting an import or a location does not take the lock, so a delete can make an import wait.
- The writer relies on EF Core's default table and column names. A renamed property or table needs a matching change in `TideEntryWriter`.
- Integration tests guard both risks. Concurrent imports for different locations all succeed, and the foreign key is still trusted after an import.
