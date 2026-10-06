using Microsoft.Extensions.Options;

namespace Tides.Api.Kartverket;

public static class KartverketServiceCollectionExtensions
{
    public static IServiceCollection AddKartverket(this IServiceCollection services)
    {
        services.AddOptions<KartverketOptions>()
            .BindConfiguration(KartverketOptions.SectionName)
            .Validate(
                options => options.BaseAddress is { IsAbsoluteUri: true, Scheme: "http" or "https" },
                "Kartverket:BaseAddress must be an absolute http or https address.")
            .ValidateOnStart();

        services.AddHttpClient<KartverketClient>((provider, client) =>
            client.BaseAddress = provider.GetRequiredService<IOptions<KartverketOptions>>().Value.BaseAddress);

        return services;
    }
}
