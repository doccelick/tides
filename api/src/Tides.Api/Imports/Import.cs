namespace Tides.Api.Imports;

public sealed class Import
{
    public const int MaxDays = 31;

    public int Id { get; set; }
    public int LocationId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    public List<TideEntry> Entries { get; set; } = [];

    public bool Covers(DateTimeOffset time) => From <= time && time < To;
}
