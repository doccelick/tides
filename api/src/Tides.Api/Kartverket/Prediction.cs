namespace Tides.Api.Kartverket;

public sealed record Prediction(
    DateTimeOffset Time,
    double LevelCm
);
