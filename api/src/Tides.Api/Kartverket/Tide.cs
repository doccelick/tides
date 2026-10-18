namespace Tides.Api.Kartverket;

public sealed record Tide(
    TideType Type,
    DateTimeOffset Time,
    double LevelCm
);
