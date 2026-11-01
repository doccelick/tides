using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tides.Api.Data;
using Tides.Api.Imports;

namespace Tides.Api.IntegrationTests.Imports;

public class ImportEndpointsTests : IAsyncDisposable
{
    private static readonly DateTimeOffset From = new(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddMinutes(20);

    // The clock is in November, so the window runs from 1 October through 31 December.
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 00, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowFrom = new(2026, 10, 01, 00, 00, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowTo = new(2027, 01, 01, 00, 00, 0, TimeSpan.Zero);

    private readonly FakeKartverketHandler _handler = new(KartverketXml.Tides, KartverketXml.Predictions);
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public ImportEndpointsTests(SqlServerFixture database)
    {
        _factory = new TidesApiFactory(database, _handler, new FakeTimeProvider(Now));
        _client = _factory.CreateSignedInClient("Admin");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task CreateImport_NothingImported_ReturnsCreatedWindow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var importResponse = await _client.PostAsync($"/v1/locations/{location.Id}/imports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var response = await importResponse.Content.ReadFromJsonAsync<ImportsResponse>(cancellationToken);
        Assert.NotNull(response);
        var import = Assert.Single(response.Imports);
        Assert.Equal(location.Id, import.LocationId);
        Assert.Equal(WindowFrom, import.From);
        Assert.Equal(WindowTo, import.To);
        Assert.Equal(4, import.EntryCount);
    }

    [Fact]
    public async Task CreateImport_MiddleMonthImported_ReturnsCreatedMonthsOnBothSides()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);
        var november = (From: WindowFrom.AddMonths(1), To: WindowFrom.AddMonths(2));

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            await service.ImportAsync(location, november.From, november.To, cancellationToken);
        }

        var importResponse = await _client.PostAsync($"/v1/locations/{location.Id}/imports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var response = await importResponse.Content.ReadFromJsonAsync<ImportsResponse>(cancellationToken);
        Assert.NotNull(response);
        Assert.Equal(2, response.Imports.Count);
        Assert.Equal((WindowFrom, november.From), (response.Imports[0].From, response.Imports[0].To));
        Assert.Equal((november.To, WindowTo), (response.Imports[1].From, response.Imports[1].To));
    }

    [Fact]
    public async Task CreateImport_WindowImported_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        await _client.PostAsync($"/v1/locations/{location.Id}/imports", null, cancellationToken);

        var second = await _client.PostAsync($"/v1/locations/{location.Id}/imports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(2, _handler.RequestCount);
    }

    [Fact]
    public async Task CreateImport_UnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsync($"/v1/locations/{int.MaxValue}/imports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _handler.RequestCount);
    }

    [Fact]
    public async Task GetImports_AfterCreate_ReturnsCreatedImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        var importResponse = await _client.PostAsync($"/v1/locations/{location.Id}/imports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var getResponse = await _client.GetFromJsonAsync<ImportsResponse>(
            $"/v1/locations/{location.Id}/imports",
            cancellationToken
        );

        Assert.NotNull(getResponse);
        var import = Assert.Single(getResponse.Imports);
        Assert.Equal(4, import.EntryCount);
        Assert.Equal(WindowFrom, import.From);
        Assert.Equal(WindowTo, import.To);
    }

    [Fact]
    public async Task GetImports_UnknownLocation_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var getResponse = await _client.GetAsync(
            $"/v1/locations/{int.MaxValue}/imports",
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteImport_WithEntries_DeletesImportedData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await _factory.AddLocationAsync(cancellationToken);

        Import import;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            import = Assert.Single(await service.ImportAsync(location, From, To, cancellationToken));
        }

        var deleteResponse = await _client.DeleteAsync(
            $"/v1/imports/{import.Id}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var hasEntries = await context.TideEntries.AnyAsync(e => e.ImportId == import.Id, cancellationToken);

            Assert.False(hasEntries);
        }
    }

    [Fact]
    public async Task DeleteImport_UnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var deleteResponse = await _client.DeleteAsync(
            $"/v1/imports/{int.MaxValue}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
