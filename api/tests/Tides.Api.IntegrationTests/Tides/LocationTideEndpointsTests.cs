using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Imports;
using Tides.Api.Kartverket;
using Tides.Api.Locations;
using Tides.Api.Tides;

namespace Tides.Api.IntegrationTests.Tides;

public class LocationTideEndpointsTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Today = new(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);

    private readonly FakeKartverketHandler _handler = new(KartverketXml.Tides, KartverketXml.Predictions);
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public LocationTideEndpointsTests(SqlServerFixture database)
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
    public async Task GetLocationTides_UnknownId_ReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync($"/v1/locations/{int.MaxValue}/tides", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("Location not found.", problem.Title);
        Assert.Equal(0, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_KnownLocation_ReturnsNoCacheWithETag()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/tides", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.Null(response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task GetLocationTides_ImportedWindow_ReturnsStoredHighsAndLows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(2), cancellationToken);
        var requestsAfterImport = _handler.RequestCount;

        var response = await _client.GetFromJsonAsync<TidesResponse>($"/v1/locations/{location.Id}/tides", cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(TideSource.Database, response.Source);
        var tide = Assert.Single(response.Tides);
        Assert.Equal(new TideResponse(TideType.High, Today.AddMinutes(5), 250.0), tide);
        Assert.Equal(requestsAfterImport, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_NoImports_FetchesFromKartverket()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var response = await _client.GetFromJsonAsync<TidesResponse>($"/v1/locations/{location.Id}/tides", cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(TideSource.Kartverket, response.Source);
        var tide = Assert.Single(response.Tides);
        Assert.Equal(new TideResponse(TideType.High, Today.AddMinutes(5), 250.0), tide);
        Assert.Equal(1, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_PartlyImportedWindow_FetchesFromKartverket()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(1), cancellationToken);
        var requestsAfterImport = _handler.RequestCount;

        var response = await _client.GetFromJsonAsync<TidesResponse>($"/v1/locations/{location.Id}/tides", cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(TideSource.Kartverket, response.Source);
        Assert.Single(response.Tides);
        Assert.Equal(requestsAfterImport + 1, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_ImportedRangeInPast_ReturnsStoredHighsAndLows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today.AddDays(-5), Today.AddDays(1), cancellationToken);
        var requestsAfterImport = _handler.RequestCount;

        var response = await _client.GetFromJsonAsync<TidesResponse>(
            $"/v1/locations/{location.Id}/tides?from=2026-10-27&days=6",
            cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(TideSource.Database, response.Source);
        var tide = Assert.Single(response.Tides);
        Assert.Equal(new TideResponse(TideType.High, Today.AddMinutes(5), 250.0), tide);
        Assert.Equal(requestsAfterImport, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_RangePastImportedWindow_FetchesFromKartverket()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        await ImportAsync(location, Today, Today.AddDays(2), cancellationToken);
        var requestsAfterImport = _handler.RequestCount;

        var response = await _client.GetFromJsonAsync<TidesResponse>(
            $"/v1/locations/{location.Id}/tides?days=7",
            cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(TideSource.Kartverket, response.Source);
        Assert.Single(response.Tides);
        Assert.Equal(requestsAfterImport + 1, _handler.RequestCount);
    }

    [Fact]
    public async Task GetLocationTides_RangeEndingAfterLastRepresentableDay_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var response = await _client.GetAsync($"/v1/locations/{location.Id}/tides?from=9999-12-31", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.Contains("from", body!.Errors.Keys);
        Assert.Equal(0, _handler.RequestCount);
    }
}
