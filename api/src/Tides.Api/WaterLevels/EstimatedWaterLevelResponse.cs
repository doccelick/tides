namespace Tides.Api.WaterLevels;

public sealed record EstimatedWaterLevelResponse(
    int LocationId,
    string TimeZone,
    string Unit,
    string Reference,
    DateTimeOffset Time,
    double LevelCm,
    string StationCode,
    string StationName,
    double DistanceKm,
    double WeatherEffectCm
);
