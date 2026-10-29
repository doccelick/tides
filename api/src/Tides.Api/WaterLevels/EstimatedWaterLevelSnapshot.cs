namespace Tides.Api.WaterLevels;

public sealed record EstimatedWaterLevelSnapshot(
    EstimatedWaterLevelResponse? Response,
    string ETag
);
