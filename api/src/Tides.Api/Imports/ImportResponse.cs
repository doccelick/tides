namespace Tides.Api.Imports;

public sealed record ImportResponse(
    int Id,
    int LocationId,
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset ImportedAt,
    int EntryCount
);
