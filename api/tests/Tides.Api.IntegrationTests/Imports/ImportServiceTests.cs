using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Imports;

public class ImportServiceTests(SqlServerFixture database)
{
    [Fact]
    public async Task ImportAsync_NewRange_StoresEntriesBeforeTo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        Import? import;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
            import = await scope.ServiceProvider.GetRequiredService<ImportService>().ImportAsync(location, from, to, cancellationToken);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var entries = await context.TideEntries
                .Where(e => e.ImportId == import!.Id)
                .OrderBy(e => e.Time)
                .ToListAsync(cancellationToken);

            Assert.Equal(3, entries.Count);
            Assert.Equal((TideEntryType.Prediction, from), (entries[0].Type, entries[0].Time));
            Assert.Equal((TideEntryType.High, from.AddMinutes(5)), (entries[1].Type, entries[1].Time));
            Assert.Equal((TideEntryType.Prediction, from.AddMinutes(10)), (entries[2].Type, entries[2].Time));
        }
    }

    [Fact]
    public async Task ImportAsync_OverlappingRange_ReturnsNullWithoutFetching()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            await service.ImportAsync(location, from, to, cancellationToken);
            var second = await service.ImportAsync(location, from.AddMinutes(10), to.AddMinutes(10), cancellationToken);

            Assert.Null(second);
            Assert.Equal(2, handler.RequestCount);
        }
    }

    [Fact]
    public async Task ImportAsync_EntriesAlreadyStored_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        // The stored import's range does not overlap, so only the unique index on entries catches the duplicate.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var stored = new Import { LocationId = location.Id, From = from.AddDays(30), To = to.AddDays(30), ImportedAt = from };
            stored.Entries.Add(new TideEntry { LocationId = location.Id, Time = from, Type = TideEntryType.Prediction, LevelCm = 248.0 });
            context.Imports.Add(stored);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            var import = await service.ImportAsync(location, from, to, cancellationToken);

            Assert.Null(import);
        }
    }

    [Fact]
    public async Task ImportAsync_AdjacentRange_StoresBoundaryInNextImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            await service.ImportAsync(location, from, to, cancellationToken);
            var second = await service.ImportAsync(location, to, to.AddMinutes(20), cancellationToken);

            Assert.NotNull(second);
            var entry = Assert.Single(second.Entries);
            Assert.Equal((TideEntryType.Prediction, to), (entry.Type, entry.Time));
        }
    }

    [Fact]
    public async Task DeleteImport_WithEntries_DeletesEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        Import? import;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            import = await service.ImportAsync(location, from, to, cancellationToken);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            await context.Imports.Where(i => i.Id == import!.Id).ExecuteDeleteAsync(cancellationToken);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var hasEntries = await context.TideEntries.AnyAsync(e => e.ImportId == import!.Id, cancellationToken);

            Assert.False(hasEntries);
        }
    }
}
