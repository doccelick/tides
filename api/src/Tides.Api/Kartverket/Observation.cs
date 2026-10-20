namespace Tides.Api.Kartverket;

public sealed record Observation(
    DateTimeOffset Time,
    double LevelCm
);
