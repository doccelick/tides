namespace Tides.Api.Imports;

public sealed class TideEntry
{
    public int Id { get; set; }
    public int ImportId { get; set; }
    public int LocationId { get; set; }
    public DateTimeOffset Time { get; set; }
    public TideEntryType Type { get; set; }
    public double LevelCm { get; set; }
}
