# 0001. SQL Server over SQLite

## Status

Accepted

## Context

The API stores imported tide data in a relational database through EF Core. The database runs in three places: on the developer machine, in integration tests, and in Azure. SQLite needs no server and is the simplest to start with, but it differs from SQL Server in types, date handling, concurrency and SQL dialect.

## Decision

Use SQL Server everywhere: in Docker locally, in Testcontainers for integration tests, and the Azure SQL free offer in the cloud.

## Consequences

- Local development and tests run on the same engine as production, so behavior that passes locally also holds in the cloud.
- EF Core migrations target one provider only.
- Local development needs Docker and a SQL Server container, which is heavier than a SQLite file.
- The SQL Server container won't start without an administrator password, so local development has a secret that must stay out of the repo.
