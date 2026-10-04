namespace Tides.Api.Kartverket;

public sealed class KartverketOptions
{
    public const string SectionName = "Kartverket";

    public required Uri BaseAddress { get; init; }
}
