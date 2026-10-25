using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Imports;

public class ImportEndpointsTests(SqlServerFixture database)
{
    private const string Tides = """
        <tide><locationdata><data>
            <waterlevel value="250.0" time="2026-11-01T00:05:00+00:00" flag="high"/>
        </data></locationdata></tide>
    """;

    private const string Predictions = """
        <tide><locationdata><data>
            <waterlevel value="248.0" time="2026-11-01T00:00:00+00:00" flag="pre"/>
            <waterlevel value="250.0" time="2026-11-01T00:10:00+00:00" flag="pre"/>
            <waterlevel value="249.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    [Fact]
    public async Task CreateImport_NewRange_ReturnsCreatedImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
        }

        var importResponse = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var response = await importResponse.Content.ReadFromJsonAsync<ImportResponse>(cancellationToken);
        Assert.NotNull(response);
        Assert.Equal(location.Id, response.LocationId);
        Assert.Equal(from, response.From);
        Assert.Equal(to, response.To);
        Assert.Equal(3, response.EntryCount);
    }

    [Fact]
    public async Task CreateImport_OverlappingRange_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
        }

        await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        var second = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(from.AddMinutes(10), to.AddMinutes(10)),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task CreateImport_UnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        var response = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{int.MaxValue}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(0)]
    public async Task CreateImport_InvalidRange_ReturnsValidationProblem(int toOffset)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(toOffset);

        var response = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{int.MaxValue}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("To", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateImport_RangeOverMax_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddDays(Import.MaxDays).AddMinutes(1);

        var response = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{int.MaxValue}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("To", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateImport_MissingTo_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);

        var response = await client.PostAsJsonAsync(
            $"/v1/locations/{int.MaxValue}/imports",
            new { From = from },
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("To", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetImports_AfterCreate_ReturnsCreatedImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
        }

        var importResponse = await client.PostAsJsonAsync<CreateImportRequest>(
            $"/v1/locations/{location.Id}/imports",
            new CreateImportRequest(from, to),
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var getResponse = await client.GetFromJsonAsync<ImportsResponse>(
            $"/v1/locations/{location.Id}/imports",
            cancellationToken
        );

        Assert.NotNull(getResponse);
        var import = Assert.Single(getResponse.Imports);
        Assert.Equal(3, import.EntryCount);
        Assert.Equal(from, import.From);
        Assert.Equal(to, import.To);
    }

    [Fact]
    public async Task GetImports_UnknownLocation_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var getResponse = await client.GetAsync(
            $"/v1/locations/{int.MaxValue}/imports",
            cancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteImport_WithEntries_DeletesImportedData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        Import? import;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            import = await service.ImportAsync(location, from, to, cancellationToken);
            Assert.NotNull(import);
        }

        var deleteResponse = await client.DeleteAsync(
            $"/v1/imports/{import.Id}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var deleteResponse = await client.DeleteAsync(
            $"/v1/imports/{int.MaxValue}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
