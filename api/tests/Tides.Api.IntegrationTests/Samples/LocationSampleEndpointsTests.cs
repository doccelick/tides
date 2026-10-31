using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Imports;
using Tides.Api.Locations;
using Tides.Api.Samples;

namespace Tides.Api.IntegrationTests.Samples;

public class LocationSampleEndpointsTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Today = new(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);

    private const string Predictions = """
        <tide><locationdata><data>
            <waterlevel value="248.0" time="2026-11-01T00:00:00+00:00" flag="pre"/>
            <waterlevel value="250.0" time="2026-11-01T00:10:00+00:00" flag="pre"/>
            <waterlevel value="249.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
            <waterlevel value="246.0" time="2026-11-01T00:30:00+00:00" flag="pre"/>
            <waterlevel value="241.0" time="2026-11-01T00:40:00+00:00" flag="pre"/>
            <waterlevel value="235.0" time="2026-11-01T00:50:00+00:00" flag="pre"/>
            <waterlevel value="228.0" time="2026-11-01T01:00:00+00:00" flag="pre"/>
            <waterlevel value="220.0" time="2026-11-01T01:10:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    private readonly FakeKartverketHandler _handler = new(KartverketXml.Tides, Predictions);
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public LocationSampleEndpointsTests(SqlServerFixture database)
    {
        _factory = new TidesApiFactory(database, _handler, new FakeTimeProvider(Today.AddHours(12)));
        _client = _factory.CreateClient();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task ImportAsync(Location location, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ImportService>();
        var imports = await service.ImportAsync(location, from, to, cancellationToken);

        Assert.NotEmpty(imports);
    }

    [Fact]
    public async Task GetLocationSamples_UnknownId_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync($"/v1/locations/{int.MaxValue}/samples", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("Location not found.", problem.Title);
    }

    [Fact]
    public async Task GetLocationSamples_NoImports_ReturnsNotFoundWithoutFetching()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/samples", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("No imported water levels for this range.", problem.Title);
        Assert.Equal(0, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationSamples_PartlyImportedRange_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(1), cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/samples", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationSamples_ImportedRange_ReturnsEveryStoredPrediction()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(2), cancellationToken);
        var requestsAfterImport = _handler.RequestCount;

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/samples", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.NotNull(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<SamplesResponse>(cancellationToken);
        Assert.NotNull(body);
        Assert.Equal(location.Id, body.LocationId);
        Assert.Equal("UTC", body.TimeZone);
        Assert.Equal("cm", body.Unit);
        Assert.Equal("CD", body.Reference);
        Assert.Equal(10, body.IntervalMinutes);
        Assert.Equal(8, body.Samples.Count);
        Assert.Equal(new SampleResponse(Today, 248.0), body.Samples[0]);
        Assert.Equal(new SampleResponse(Today.AddMinutes(70), 220.0), body.Samples[^1]);
        Assert.Equal(requestsAfterImport, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationSamples_SixtyMinuteInterval_ReturnsValuesOnTheHour()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(2), cancellationToken);

        var body = await _client.GetFromJsonAsync<SamplesResponse>(
            $"/v1/locations/{location.Id}/samples?interval=60",
            cancellationToken);

        Assert.NotNull(body);
        Assert.Equal(60, body.IntervalMinutes);
        Assert.Equal(
            [new SampleResponse(Today, 248.0), new SampleResponse(Today.AddHours(1), 228.0)],
            body.Samples);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public async Task GetLocationSamples_UnsupportedInterval_ReturnsValidationProblem(int interval)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/samples?interval={interval}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.Contains("interval", body!.Errors.Keys);
    }

    [Fact]
    public async Task GetLocationSamples_MatchingETag_ReturnsNotModified()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(2), cancellationToken);
        var firstResponse = await _client.GetAsync($"/v1/locations/{location.Id}/samples", cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/locations/{location.Id}/samples");
        request.Headers.IfNoneMatch.Add(firstResponse.Headers.ETag!);
        var response = await _client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(firstResponse.Headers.ETag, response.Headers.ETag);
    }
}
