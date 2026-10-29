using Tides.Api.Stations;

namespace Tides.Api.Tests.Stations;

public class GeoDistanceTests
{
    [Fact]
    public void GetKilometers_SamePoint_ReturnsZero()
    {
        var distance = GeoDistance.GetKilometers(67.29233, 14.39977, 67.29233, 14.39977);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void GetKilometers_OneDegreeOfLatitude_ReturnsArcLength()
    {
        var distance = GeoDistance.GetKilometers(60, 5, 61, 5);

        Assert.Equal(111.19, distance, precision: 2);
    }

    [Fact]
    public void GetKilometers_SwappedPoints_ReturnsSameDistance()
    {
        var distance = GeoDistance.GetKilometers(60.398046, 5.320487, 62.469414, 6.151946);
        var swapped = GeoDistance.GetKilometers(62.469414, 6.151946, 60.398046, 5.320487);

        Assert.Equal(distance, swapped, precision: 9);
    }
}
