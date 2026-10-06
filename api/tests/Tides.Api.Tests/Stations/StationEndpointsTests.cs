using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

namespace Tides.Api.Tests.Stations;

public class StationEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FakeKartverketHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        }
    }

    private static async Task<FakeKartverketHandler> CreateStationListHandlerAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "stationlist.xml");
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return new FakeKartverketHandler(HttpStatusCode.OK, content);
    }

    private WebApplicationFactory<Program> CreateApp(FakeKartverketHandler handler) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => handler)));

    [Fact]
    public async Task GetStations_KartverketResponds_ReturnsStationsWithCacheHeaders()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateStationListHandlerAsync(cancellationToken));
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);

        var body = await response.Content.ReadFromJsonAsync<StationsResponse>(cancellationToken);
        Assert.Equal(3, body!.Stations.Count);
        Assert.Equal(new StationResponse("BOO", "Bodø", 67.29233, 14.39977), body.Stations[1]);
    }

    [Fact]
    public async Task GetStations_MatchingIfNoneMatch_ReturnsNotModified()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateStationListHandlerAsync(cancellationToken));
        var client = app.CreateClient();
        var firstResponse = await client.GetAsync("/v1/stations", cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/stations");
        request.Headers.IfNoneMatch.Add(firstResponse.Headers.ETag!);
        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(firstResponse.Headers.ETag, response.Headers.ETag);
    }

    [Fact]
    public async Task GetStations_CalledTwice_FetchesFromKartverketOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = await CreateStationListHandlerAsync(cancellationToken);
        await using var app = CreateApp(handler);
        var client = app.CreateClient();

        await client.GetAsync("/v1/stations", cancellationToken);
        await client.GetAsync("/v1/stations", cancellationToken);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetStations_KartverketFails_ReturnsBadGatewayProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(new FakeKartverketHandler(HttpStatusCode.InternalServerError, string.Empty));
        var client = app.CreateClient();

        var response = await client.GetAsync("/v1/stations", cancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetStations_AllowedOrigin_ReturnsCorsHeader()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateStationListHandlerAsync(cancellationToken));
        var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/stations");
        request.Headers.Add("Origin", "http://localhost:5173");
        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("ETag", Assert.Single(response.Headers.GetValues("Access-Control-Expose-Headers")));
    }

    [Fact]
    public async Task GetStations_PreflightWithIfNoneMatch_AllowsHeader()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateStationListHandlerAsync(cancellationToken));
        var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/v1/stations");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "if-none-match");
        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("if-none-match", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Headers")), ignoreCase: true);
    }

    [Fact]
    public async Task GetStations_UnknownOrigin_ReturnsNoCorsHeader()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = CreateApp(await CreateStationListHandlerAsync(cancellationToken));
        var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/stations");
        request.Headers.Add("Origin", "http://unknown.test");
        var response = await client.SendAsync(request, cancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
