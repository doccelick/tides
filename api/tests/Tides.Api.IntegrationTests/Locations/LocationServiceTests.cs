using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Locations;

public class LocationServiceTests(SqlServerFixture database)
{
    [Fact]
    public async Task IsNearCoastAsync_LocationInland_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.NoData);
        await using var factory = new TidesApiFactory(database, handler);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<LocationService>();
            var isNearCoast = await service.IsNearCoastAsync(61.540, 10.168, cancellationToken);

            Assert.False(isNearCoast);
        }
    }

    [Fact]
    public async Task IsNearCoastAsync_LocationNearCoast_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions);
        await using var factory = new TidesApiFactory(database, handler);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<LocationService>();
            var isNearCoast = await service.IsNearCoastAsync(67.29, 14.40, cancellationToken);

            Assert.True(isNearCoast);
        }
    }
}
