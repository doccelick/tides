using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Kartverket;
using Tides.Api.Tides;

namespace Tides.Api.Tests.Tides;

public class TideEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FakeKartverketHandler(string stationList, string tides) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isStationList = request.RequestUri!.Query.Contains("tide_request=stationlist");
            if (!isStationList)
            {
                RequestCount++;
            }

            var content = isStationList ? stationList : tides;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
        }
    }

    private static async Task<FakeKartverketHandler> CreateHandlerAsync(string tidesFile, CancellationToken cancellationToken)
    {
        var stationListPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "stationlist.xml");
        var stationList = await File.ReadAllTextAsync(stationListPath, cancellationToken);
        var tidesPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", tidesFile);
        var tides = await File.ReadAllTextAsync(tidesPath, cancellationToken);
        return new FakeKartverketHandler(stationList, tides);
    }

    private WebApplicationFactory<Program> CreateApp(FakeKartverketHandler handler, FakeTimeProvider timeProvider) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddSingleton<TimeProvider>(timeProvider);
        }));

    [Fact]
    public async Task GetStationTides_KnownStation_ReturnsTidesWithCacheHeaders()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("locationdata.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<TidesResponse>(cancellationToken);
        Assert.Equal(4, body!.Tides.Count);
        Assert.Equal(new TideResponse(TideType.Low, new DateTimeOffset(2026, 10, 18, 9, 45, 0, TimeSpan.Zero), 142.6), body.Tides[1]);
    }

    [Fact]
    public async Task GetStationTides_UnknownStation_ReturnsNotFoundProblem()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("locationdata.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/XYZ/tides", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Station not found.", body!.Title);
    }

    [Fact]
    public async Task GetStationTides_NoData_ReturnsNotFoundProblem()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("locationdata-nodata.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("No tide data for this station.", body!.Title);
    }

    [Fact]
    public async Task GetStationTides_CalledTwiceSameDay_FetchesFromKartverketOnce()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = await CreateHandlerAsync("locationdata.xml", cancellationToken);
        await using var app = CreateApp(handler, timeProvider);
        var client = app.CreateClient();

        await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);
        await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetStationTides_CalledOnNextDay_FetchesFromKartverketAgain()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = await CreateHandlerAsync("locationdata.xml", cancellationToken);
        await using var app = CreateApp(handler, timeProvider);
        var client = app.CreateClient();

        await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);
        timeProvider.Advance(TimeSpan.FromDays(1));
        await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetStationTides_MatchingIfNoneMatch_ReturnsNotModified()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateHandlerAsync("locationdata.xml", cancellationToken), timeProvider);
        var client = app.CreateClient();
        var firstResponse = await client.GetAsync("/v1/stations/BOO/tides", cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/stations/BOO/tides");
        request.Headers.IfNoneMatch.Add(firstResponse.Headers.ETag!);
        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(firstResponse.Headers.ETag, response.Headers.ETag);
    }
}
