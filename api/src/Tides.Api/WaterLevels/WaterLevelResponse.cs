namespace Tides.Api.WaterLevels;

public sealed record WaterLevelResponse(
    string StationCode,
    string TimeZone,
    string Unit,
    string Reference,
    DateTimeOffset Time,
    double LevelCm
);
