using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Data;

public class TidesDbContextTests(SqlServerFixture database)
{
    [Fact]
    public async Task SaveChangesAsync_NewLocation_StoresLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var stored = await context.Locations.SingleAsync(l => l.Id == location.Id, cancellationToken);

            Assert.Equal("Bodø", stored.Name);
            Assert.Equal(67.29, stored.Latitude);
            Assert.Equal(14.40, stored.Longitude);
        }
    }
}
