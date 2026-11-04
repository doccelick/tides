using Tides.Api.Imports;

namespace Tides.Api.Tests.Imports;

public class ImportWindowTests
{
    private static DateTimeOffset Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Around_MidMonth_ReturnsPreviousThroughNextMonth()
    {
        var window = ImportWindow.Around(new DateTimeOffset(2026, 11, 15, 13, 45, 0, TimeSpan.Zero));

        Assert.Equal((Utc(2026, 10, 1), Utc(2027, 1, 1)), window);
    }

    [Fact]
    public void Around_StartOfMonth_ReturnsSameWindowAsMidMonth()
    {
        var window = ImportWindow.Around(Utc(2026, 11, 1));

        Assert.Equal((Utc(2026, 10, 1), Utc(2027, 1, 1)), window);
    }

    [Fact]
    public void Around_January_CrossesYears()
    {
        var window = ImportWindow.Around(Utc(2027, 1, 10));

        Assert.Equal((Utc(2026, 12, 1), Utc(2027, 3, 1)), window);
    }

    [Fact]
    public void Around_LocalTimeInNextMonth_UsesUtcMonth()
    {
        // 00:30 on 1 December at UTC+1 is still 30 November in UTC.
        var window = ImportWindow.Around(new DateTimeOffset(2026, 12, 1, 0, 30, 0, TimeSpan.FromHours(1)));

        Assert.Equal((Utc(2026, 10, 1), Utc(2027, 1, 1)), window);
    }
}
