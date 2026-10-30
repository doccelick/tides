using Tides.Api.Imports;

namespace Tides.Api.Tests.Imports;

public class ImportGapsTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);

    private static DateTimeOffset Day(int day) => Start.AddDays(day);

    [Fact]
    public void Find_NoImports_ReturnsWholeRange()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), []);

        Assert.Equal([(Day(0), Day(10))], gaps);
    }

    [Fact]
    public void Find_ImportCoversStart_ReturnsEnd()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(-5), Day(3))]);

        Assert.Equal([(Day(3), Day(10))], gaps);
    }

    [Fact]
    public void Find_ImportCoversEnd_ReturnsStart()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(8), Day(15))]);

        Assert.Equal([(Day(0), Day(8))], gaps);
    }

    [Fact]
    public void Find_ImportInMiddle_ReturnsBothSides()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(3), Day(7))]);

        Assert.Equal([(Day(0), Day(3)), (Day(7), Day(10))], gaps);
    }

    [Fact]
    public void Find_AdjacentImportsCoverRange_ReturnsEmpty()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(0), Day(4)), (Day(4), Day(10))]);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Find_UnorderedImports_ReturnsGapsInOrder()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(6), Day(8)), (Day(2), Day(4))]);

        Assert.Equal([(Day(0), Day(2)), (Day(4), Day(6)), (Day(8), Day(10))], gaps);
    }

    [Fact]
    public void Find_ImportOutsideRange_ReturnsWholeRange()
    {
        var gaps = ImportGaps.Find(Day(0), Day(10), [(Day(-10), Day(0)), (Day(10), Day(20))]);

        Assert.Equal([(Day(0), Day(10))], gaps);
    }
}
