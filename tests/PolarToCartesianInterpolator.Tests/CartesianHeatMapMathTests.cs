using Xunit;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.Tests;

public sealed class CartesianHeatMapMathTests
{
    [Fact]
    public void SubtractFromOne_ReturnsOneMinusEachCell()
    {
        var input = new float[,]
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
        var left = new float[,]
        {
            { 1.0, 0.5 },
            { 0.2, 0.8 }
        };
        var right = new float[,]
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
        var left = new float[,] { { 1.0, 0.5 } };
        var right = new float[,]
        {
            { 0.1, 0.4 },
            { 0.5, 0.25 }
        };

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.MultiplyElementWise(left, right));
    }

    [Fact]
    public void SubtractFromOne_WithEmptyHeatMap_Throws()
    {
        var input = new float[0, 0];

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.SubtractFromOne(input));
    }

    [Fact]
    public void CalculateTemperatureTimesArea_UsesAverageOfFourCornersPerCell()
    {
        var input = new float[,]
        {
            { 0.2, 0.4 },
            { 0.6, 0.8 }
        };

        var result = CartesianHeatMapMath.CalculateTemperatureTimesArea(input, cellSize: 2.0);

        Assert.Equal(2.0, result, 6);
    }

    [Fact]
    public void CalculateTemperatureTimesArea_WithTooSmallGrid_Throws()
    {
        var input = new float[,] { { 0.2 } };

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.CalculateTemperatureTimesArea(input));
    }

    [Fact]
    public void ApplyBinaryThreshold_MapsValuesToZeroAndOne()
    {
        var input = new float[,]
        {
            { 0.49, 0.50 },
            { 0.90, 0.10 }
        };

        var result = CartesianHeatMapMath.ApplyBinaryThreshold(input, threshold: 0.5);

        Assert.Equal(0.0, result[0, 0], 6);
        Assert.Equal(1.0, result[0, 1], 6);
        Assert.Equal(1.0, result[1, 0], 6);
        Assert.Equal(0.0, result[1, 1], 6);
    }

    [Fact]
    public void CalculateThresholdedTemperatureTimesArea_UsesThresholdedBinaryGrid()
    {
        var input = new float[,]
        {
            { 0.49, 0.50 },
            { 0.90, 0.10 }
        };

        var result = CartesianHeatMapMath.CalculateThresholdedTemperatureTimesArea(input, threshold: 0.5, cellSize: 2.0);

        Assert.Equal(2.0, result, 6);
    }

    [Fact]
    public void CalculateThresholdedTemperatureTimesArea_WithInvalidThreshold_Throws()
    {
        var input = new float[,]
        {
            { 0.2, 0.4 },
            { 0.6, 0.8 }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CartesianHeatMapMath.CalculateThresholdedTemperatureTimesArea(input, threshold: 1.1));
    }
}
