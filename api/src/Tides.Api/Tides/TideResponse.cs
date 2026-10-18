using Tides.Api.Kartverket;

namespace Tides.Api.Tides;

public sealed record TideResponse(
    TideType Type,
    DateTimeOffset Time,
    double LevelCm
);
