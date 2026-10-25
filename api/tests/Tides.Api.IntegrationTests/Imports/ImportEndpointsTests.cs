using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Imports;

public class ImportEndpointsTests : IAsyncDisposable
{
    private static readonly DateTimeOffset From = new(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddMinutes(20);

    private readonly FakeKartverketHandler _handler = new(KartverketXml.Tides, KartverketXml.Predictions);
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public ImportEndpointsTests(SqlServerFixture database)
    {
        _factory = new TidesApiFactory(database, _handler);
        _client = _factory.CreateClient();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<Location> AddLocationAsync(CancellationToken cancellationToken)
    {
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);

        return location;
    }

    [Fact]
    public async Task CreateImport_NewRange_ReturnsCreatedImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(cancellationToken);

        var importResponse = await _client.PostAsJsonAsync(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(From, To),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var response = await importResponse.Content.ReadFromJsonAsync<ImportResponse>(cancellationToken);
        Assert.NotNull(response);
        Assert.Equal(location.Id, response.LocationId);
        Assert.Equal(From, response.From);
        Assert.Equal(To, response.To);
        Assert.Equal(3, response.EntryCount);
    }

    [Fact]
    public async Task CreateImport_OverlappingRange_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(cancellationToken);

        await _client.PostAsJsonAsync(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(From, To),
            cancellationToken
        );

        var second = await _client.PostAsJsonAsync(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(From.AddMinutes(10), To.AddMinutes(10)),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(2, _handler.RequestCount);
    }

    [Fact]
    public async Task CreateImport_UnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync(
            $"/v1/locations/{int.MaxValue}/imports",
            new CreateImportRequest(From, To),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _handler.RequestCount);
    }

    public static TheoryData<object> InvalidCreateRequests => new()
    {
        new { From, To = From.AddMinutes(-20) },
        new { From, To = From },
        new { From, To = From.AddDays(Import.MaxDays).AddMinutes(1) },
        new { From },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public async Task CreateImport_InvalidRequest_ReturnsValidationProblemForTo(object request)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync($"/v1/locations/{int.MaxValue}/imports", request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("To", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetImports_AfterCreate_ReturnsCreatedImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var location = await AddLocationAsync(cancellationToken);

        var importResponse = await _client.PostAsJsonAsync(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(From, To),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var getResponse = await _client.GetFromJsonAsync<ImportsResponse>(
            $"/v1/locations/{location.Id}/imports",
            cancellationToken
        );

        Assert.NotNull(getResponse);
        var import = Assert.Single(getResponse.Imports);
        Assert.Equal(3, import.EntryCount);
        Assert.Equal(From, import.From);
        Assert.Equal(To, import.To);
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
        var location = await AddLocationAsync(cancellationToken);

        Import? import;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            import = await service.ImportAsync(location, From, To, cancellationToken);
            Assert.NotNull(import);
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
