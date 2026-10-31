using Microsoft.AspNetCore.Http;
using Tides.Api.Http;

namespace Tides.Api.Tests.Http;

public class ETagsTests
{
    private sealed record Sample(string Name, double LevelCm);

    [Fact]
    public void Compute_EqualValues_ReturnsSameQuotedETag()
    {
        var eTag = ETags.Compute(new Sample("BOO", 187.3));
        var same = ETags.Compute(new Sample("BOO", 187.3));

        Assert.Equal(eTag, same);
        Assert.StartsWith("\"", eTag);
        Assert.EndsWith("\"", eTag);
    }

    [Fact]
    public void Compute_DifferentValues_ReturnsDifferentETags()
    {
        var eTag = ETags.Compute(new Sample("BOO", 187.3));
        var other = ETags.Compute(new Sample("BOO", 187.4));

        Assert.NotEqual(eTag, other);
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("W/\"abc\"")]
    [InlineData("*")]
    [InlineData("\"other\", \"abc\"")]
    public void Matches_IfNoneMatchHoldsETag_ReturnsTrue(string ifNoneMatch)
    {
        var request = CreateRequest(ifNoneMatch);

        Assert.True(ETags.Matches(request, "\"abc\""));
    }

    [Fact]
    public void Matches_OtherETag_ReturnsFalse()
    {
        var request = CreateRequest("\"other\"");

        Assert.False(ETags.Matches(request, "\"abc\""));
    }

    [Fact]
    public void Matches_NoIfNoneMatch_ReturnsFalse()
    {
        var request = new DefaultHttpContext().Request;

        Assert.False(ETags.Matches(request, "\"abc\""));
    }

    private static HttpRequest CreateRequest(string ifNoneMatch)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.IfNoneMatch = ifNoneMatch;

        return request;
    }
}
