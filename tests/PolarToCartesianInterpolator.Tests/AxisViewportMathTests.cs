using Xunit;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.Tests;

public sealed class AxisViewportMathTests
{
    [Fact]
    public void MapContentXToCartesian_MapsLeftCenterRightToExpectedValues()
    {
        const double drawLeft = 10;
        const double drawWidth = 100;
        const double halfRange = 50;

        Assert.Equal(-50, AxisViewportMath.MapContentXToCartesian(drawLeft, drawWidth, halfRange, 10), 6);
        Assert.Equal(0, AxisViewportMath.MapContentXToCartesian(drawLeft, drawWidth, halfRange, 60), 6);
        Assert.Equal(50, AxisViewportMath.MapContentXToCartesian(drawLeft, drawWidth, halfRange, 110), 6);
    }

    [Fact]
    public void MapContentYToCartesian_MapsTopCenterBottomToExpectedValues()
    {
        const double drawTop = 20;
        const double drawHeight = 200;
        const double halfRange = 80;

        Assert.Equal(80, AxisViewportMath.MapContentYToCartesian(drawTop, drawHeight, halfRange, 20), 6);
        Assert.Equal(0, AxisViewportMath.MapContentYToCartesian(drawTop, drawHeight, halfRange, 120), 6);
        Assert.Equal(-80, AxisViewportMath.MapContentYToCartesian(drawTop, drawHeight, halfRange, 220), 6);
    }

    [Theory]
    [InlineData(0, 10, 0.0, 0)]
    [InlineData(0, 10, 0.5, 5)]
    [InlineData(0, 10, 1.0, 10)]
    [InlineData(10, 20, 0.25, 12.5)]
    public void Interpolate_ReturnsLinearInterpolation(double start, double end, double t, double expected)
    {
        Assert.Equal(expected, AxisViewportMath.Interpolate(start, end, t), 6);
    }
}
