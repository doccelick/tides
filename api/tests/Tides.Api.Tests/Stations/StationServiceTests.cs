using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

namespace Tides.Api.Tests.Stations;

public class StationServiceTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FakeKartverketHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
    }

    private async Task<WebApplicationFactory<Program>> CreateAppAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "stationlist.xml");
        var handler = new FakeKartverketHandler(await File.ReadAllTextAsync(path, cancellationToken));

        return factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => handler)));
    }

    [Fact]
    public async Task GetNearestStation_StationWithinLimit_ReturnsNearestWithDistance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateAppAsync(cancellationToken);
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<StationService>();

        var nearest = await service.GetNearestStationAsync(67.28, 14.40, 50, cancellationToken);

        Assert.NotNull(nearest);
        Assert.Equal("BOO", nearest.Station.Code);
        Assert.Equal(1.37, nearest.DistanceKm, precision: 2);
    }

    [Fact]
    public async Task GetNearestStation_NoStationWithinLimit_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await CreateAppAsync(cancellationToken);
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<StationService>();

        var nearest = await service.GetNearestStationAsync(70.0, 25.0, 50, cancellationToken);

        Assert.Null(nearest);
    }
}
