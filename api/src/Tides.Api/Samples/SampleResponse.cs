namespace Tides.Api.Samples;

public sealed record SampleResponse(
    DateTimeOffset Time,
    double LevelCm
);
