using Xunit;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.Tests;

public sealed class CartesianHeatMapMathTests
{
    [Fact]
    public void FindMaximumPoint_ReturnsMaximumCellLocationAndValue()
    {
        var input = new float[,]
        {
            { 0.1f, 0.2f, 0.3f },
            { 0.4f, 0.9f, 0.5f },
            { 0.6f, 0.7f, 0.8f }
        };

        var result = CartesianHeatMapMath.FindMaximumPoint(input);

        Assert.Equal(1, result.Row);
        Assert.Equal(1, result.Column);
        Assert.Equal(0.9f, result.Value, 6);
    }

    [Fact]
    public void FindMaxSumRectangle_ReturnsTopLeftAndSumForBestWindow()
    {
        var input = new float[,]
        {
            { 1f, 1f, 1f, 1f },
            { 1f, 9f, 9f, 1f },
            { 1f, 9f, 9f, 1f },
            { 1f, 1f, 1f, 1f }
        };

        var result = CartesianHeatMapMath.FindMaxSumRectangle(input, rectangleHeight: 2, rectangleWidth: 2);

        Assert.Equal(1, result.TopRow);
        Assert.Equal(1, result.LeftColumn);
        Assert.Equal(2, result.Height);
        Assert.Equal(2, result.Width);
        Assert.Equal(36f, result.Sum, 6);
        Assert.Equal(1.5f, result.CenterRow, 6);
        Assert.Equal(1.5f, result.CenterColumn, 6);
    }

    [Fact]
    public void FindMaxSumRectangle_WhenRectangleLargerThanGrid_Throws()
    {
        var input = new float[,]
        {
            { 1f, 2f },
            { 3f, 4f }
        };

        Assert.Throws<ArgumentException>(() =>
            CartesianHeatMapMath.FindMaxSumRectangle(input, rectangleHeight: 3, rectangleWidth: 2));
    }

    [Fact]
    public void SubtractFromOne_ReturnsOneMinusEachCell()
    {
        var input = new float[,]
        {
            { 0.0f, 0.25f },
            { 0.60f, 1.0f }
        };

        var result = CartesianHeatMapMath.SubtractFromOne(input);

        Assert.Equal(1.0f, result[0, 0], 6);
        Assert.Equal(0.75f, result[0, 1], 6);
        Assert.Equal(0.40f, result[1, 0], 6);
        Assert.Equal(0.0f, result[1, 1], 6);
    }

    [Fact]
    public void MultiplyElementWise_WithSameDimensions_MultipliesEachCell()
    {
        var left = new float[,]
        {
            { 1.0f, 0.5f },
            { 0.2f, 0.8f }
        };
        var right = new float[,]
        {
            { 0.1f, 0.4f },
            { 0.5f, 0.25f }
        };

        var result = CartesianHeatMapMath.MultiplyElementWise(left, right);

        Assert.Equal(0.1f, result[0, 0], 6);
        Assert.Equal(0.2f, result[0, 1], 6);
        Assert.Equal(0.1f, result[1, 0], 6);
        Assert.Equal(0.2f, result[1, 1], 6);
    }

    [Fact]
    public void MultiplyElementWise_WithDifferentDimensions_Throws()
    {
        var left = new float[,] { { 1.0f, 0.5f } };
        var right = new float[,]
        {
            { 0.1f, 0.4f },
            { 0.5f, 0.25f }
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
            { 0.2f, 0.4f },
            { 0.6f, 0.8f }
        };

        var result = CartesianHeatMapMath.CalculateTemperatureTimesArea(input, cellSize: 2.0f);

        Assert.Equal(2.0f, result, 6);
    }

    [Fact]
    public void CalculateTemperatureTimesArea_WithTooSmallGrid_Throws()
    {
        var input = new float[,] { { 0.2f } };

        Assert.Throws<ArgumentException>(() => CartesianHeatMapMath.CalculateTemperatureTimesArea(input));
    }

    [Fact]
    public void ApplyBinaryThreshold_MapsValuesToZeroAndOne()
    {
        var input = new float[,]
        {
            { 0.49f, 0.50f },
            { 0.90f, 0.10f }
        };

        var result = CartesianHeatMapMath.ApplyBinaryThreshold(input, threshold: 0.5f);

        Assert.Equal(0.0f, result[0, 0], 6);
        Assert.Equal(1.0f, result[0, 1], 6);
        Assert.Equal(1.0f, result[1, 0], 6);
        Assert.Equal(0.0f, result[1, 1], 6);
    }

    [Fact]
    public void CalculateThresholdedTemperatureTimesArea_UsesThresholdedBinaryGrid()
    {
        var input = new float[,]
        {
            { 0.49f, 0.50f },
            { 0.90f, 0.10f }
        };

        var result = CartesianHeatMapMath.CalculateThresholdedTemperatureTimesArea(input, threshold: 0.5f, cellSize: 2.0f);

        Assert.Equal(2.0f, result, 6);
    }

    [Fact]
    public void CalculateThresholdedTemperatureTimesArea_WithInvalidThreshold_Throws()
    {
        var input = new float[,]
        {
            { 0.2f, 0.4f },
            { 0.6f, 0.8f }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CartesianHeatMapMath.CalculateThresholdedTemperatureTimesArea(input, threshold: 1.1f));
    }
}
