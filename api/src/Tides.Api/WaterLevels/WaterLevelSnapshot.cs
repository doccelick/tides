namespace Tides.Api.WaterLevels;

public sealed record WaterLevelSnapshot(
    WaterLevelResponse? Response,
    string ETag
);
