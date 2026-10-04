using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Tides.Api.Tests.Kartverket;

public class KartverketOptionsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("vannstand.kartverket.no/")]
    [InlineData("ftp://vannstand.kartverket.no/")]
    public void Startup_InvalidBaseAddress_ThrowsOptionsValidationException(string baseAddress)
    {
        var invalidFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Kartverket:BaseAddress", baseAddress));

        Assert.Throws<OptionsValidationException>(() => invalidFactory.CreateClient());
    }
}
