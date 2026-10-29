using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Data;
using Tides.Api.Locations;
using Tides.Api.WaterLevels;

namespace Tides.Api.IntegrationTests.WaterLevels;

public class LocationWaterLevelEndpointsTests : IAsyncDisposable
{
    private const string StationList = """
        <tide><stationinfo>
            <location name="Bergen" code="BGO" latitude="60.398046" longitude="5.320487" type="PERM"/>
            <location name="Bodø" code="BOO" latitude="67.292330" longitude="14.399770" type="PERM"/>
        </stationinfo></tide>
    """;

    private const string StationObservations = """
        <tide><locationdata><data>
            <waterlevel value="258.0" time="2026-11-01T00:10:00+00:00" flag="obs"/>
            <waterlevel value="260.0" time="2026-11-01T00:20:00+00:00" flag="obs"/>
        </data></locationdata></tide>
    """;

    private const string StationPredictions = """
        <tide><locationdata><data>
            <waterlevel value="249.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
            <waterlevel value="247.0" time="2026-11-01T00:30:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    private const string LocationPredictions = """
        <tide><locationdata><data>
            <waterlevel value="230.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
            <waterlevel value="228.0" time="2026-11-01T00:30:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    private static readonly DateTimeOffset ObservationTime = new(2026, 11, 01, 00, 20, 0, TimeSpan.Zero);

    private sealed class WaterLevelKartverketHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var query = request.RequestUri!.Query;
            var content = query.Contains("tide_request=stationlist") ? StationList
                : query.Contains("datatype=obs") ? StationObservations
                : query.Contains("lat=67.29233") ? StationPredictions
                : LocationPredictions;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
        }
    }

    private readonly WaterLevelKartverketHandler _handler = new();
    private readonly FakeTimeProvider _timeProvider = new(ObservationTime.AddMinutes(5));
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public LocationWaterLevelEndpointsTests(SqlServerFixture database)
    {
        _factory = new TidesApiFactory(database, _handler, _timeProvider);
        _client = _factory.CreateClient();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<Location> AddLocationAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var location = new Location { Name = "Saltstraumen", Latitude = latitude, Longitude = longitude };

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);

        return location;
    }

    [Fact]
    public async Task GetLocationWaterLevel_StationNearby_ReturnsEstimateWithCacheHeaders()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(67.25, 14.40, cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromMinutes(5), response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<EstimatedWaterLevelResponse>(cancellationToken);
        var expected = new EstimatedWaterLevelResponse(
            location.Id, "UTC", "cm", "CD", ObservationTime, 241.0, "BOO", "Bodø", 4.7, 11.0);
        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task GetLocationWaterLevel_UnknownId_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync($"/v1/locations/{int.MaxValue}/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Location not found.", problem!.Title);
        Assert.Equal(0, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationWaterLevel_NoStationWithinLimit_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(70.0, 25.0, cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("No water level estimate for this location.", problem!.Title);
    }

    [Fact]
    public async Task GetLocationWaterLevel_ObservationOlderThanThirtyMinutes_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(67.25, 14.40, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(30));

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/water-level", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("No water level estimate for this location.", problem!.Title);
    }
}
