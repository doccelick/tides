using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Kartverket;

namespace Tides.Api.IntegrationTests;

public sealed class TidesApiFactory(SqlServerFixture database, HttpMessageHandler? kartverketHandler = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Tides", database.ConnectionString);

        if (kartverketHandler is not null)
        {
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => kartverketHandler));
        }
    }
}
