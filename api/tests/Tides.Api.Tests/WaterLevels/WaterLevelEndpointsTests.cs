using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Kartverket;
using Tides.Api.WaterLevels;

namespace Tides.Api.Tests.WaterLevels;

public class WaterLevelEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FakeKartverketHandler(string stationList, string observations) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isStationList = request.RequestUri!.Query.Contains("tide_request=stationlist");
            var content = isStationList ? stationList : observations;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
        }
    }

    private static async Task<FakeKartverketHandler> CreateHandlerAsync(string observationsFile, CancellationToken cancellationToken)
    {
        var stationListPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "stationlist.xml");
        var stationList = await File.ReadAllTextAsync(stationListPath, cancellationToken);
        var observationsPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", observationsFile);
        var observations = await File.ReadAllTextAsync(observationsPath, cancellationToken);
        return new FakeKartverketHandler(stationList, observations);
    }

    private WebApplicationFactory<Program> CreateApp(FakeKartverketHandler handler, FakeTimeProvider timeProvider) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddSingleton<TimeProvider>(timeProvider);
        }));

    [Fact]
    public async Task GetWaterLevel_RecentObservation_ReturnsLevelWithCacheHeaders()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 39, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("observations.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/BOO/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromMinutes(5), response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<WaterLevelResponse>(cancellationToken);
        var expected = new WaterLevelResponse("BOO", "UTC", "cm", "CD", new DateTimeOffset(2026, 10, 20, 20, 30, 0, TimeSpan.Zero), 204.0);
        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task GetWaterLevel_UnknownStation_ReturnsNotFoundProblem()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 39, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("observations.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/XYZ/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Station not found.", body!.Title);
    }

    [Fact]
    public async Task GetWaterLevel_NoData_ReturnsNotFoundProblem()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 39, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("locationdata-nodata.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/BOO/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("No water level for this station.", body!.Title);
    }

    [Fact]
    public async Task GetWaterLevel_ObservationOlderThanThirtyMinutes_ReturnsNotFoundProblem()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 21, 1, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("observations.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/BOO/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("No water level for this station.", body!.Title);
    }
}
