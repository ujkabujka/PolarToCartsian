using Xunit;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.Tests;

public sealed class CartesianHeatMapMathTests
{
    [Fact]
    public void SubtractFromOne_ReturnsOneMinusEachCell()
    {
        var input = new double[,]
        {
            { 0.0, 0.25 },
            { 0.60, 1.0 }
        };

        var result = CartesianHeatMapMath.SubtractFromOne(input);

        Assert.Equal(1.0, result[0, 0], 6);
        Assert.Equal(0.75, result[0, 1], 6);
        Assert.Equal(0.40, result[1, 0], 6);
        Assert.Equal(0.0, result[1, 1], 6);
    }

    [Fact]
    public void MultiplyElementWise_WithSameDimensions_MultipliesEachCell()
    {
        var left = new double[,]
        {
            { 1.0, 0.5 },
            { 0.2, 0.8 }
        };
        var right = new double[,]
        {
            { 0.1, 0.4 },
            { 0.5, 0.25 }
        };

        var result = CartesianHeatMapMath.MultiplyElementWise(left, right);

        Assert.Equal(0.1, result[0, 0], 6);
        Assert.Equal(0.2, result[0, 1], 6);
        Assert.Equal(0.1, result[1, 0], 6);
        Assert.Equal(0.2, result[1, 1], 6);
    }

    [Fact]
    public void MultiplyElementWise_WithDifferentDimensions_Throws()
    {
        var left = new double[,] { { 1.0, 0.5 } };
        var right = new double[,]
        {
            { 0.1, 0.4 },
            { 0.5, 0.25 }
        };

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.MultiplyElementWise(left, right));
    }

    [Fact]
    public void SubtractFromOne_WithEmptyHeatMap_Throws()
    {
        var input = new double[0, 0];

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.SubtractFromOne(input));
    }
}
