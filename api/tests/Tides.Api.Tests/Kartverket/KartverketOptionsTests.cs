using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class KartverketOptionsTests
{
    [Theory]
    [InlineData("vannstand.kartverket.no/")]
    [InlineData("ftp://vannstand.kartverket.no/")]
    public void Startup_InvalidBaseAddress_ThrowsOptionsValidationException(string baseAddress)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection([new("Kartverket:BaseAddress", baseAddress)]);
        builder.Services.AddKartverket();
        using var host = builder.Build();

        Assert.Throws<OptionsValidationException>(() => host.Start());
    }
}
