using Xunit;
using PolarToCartesianInterpolator;

namespace PolarToCartesianInterpolator.Tests;

public sealed class CartesianHeatMapControlTests
{
    [Fact]
    public void Cutoff_OutOfRange_Throws()
    {
        var sut = new CartesianHeatMapControl();

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Cutoff = -0.01);
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Cutoff = 1.01);
    }

    [Fact]
    public void BuildRenderData_BelowCutoffValue_IsWhite()
    {
        var sut = new CartesianHeatMapControl(cutoff: 0.30);
        sut.SetGrid(new float[,] { { 0.10, 0.35 } });

        var render = sut.BuildRenderData();

        Assert.Equal(HeatMapColor.White, render.Pixels[0, 0]);
        Assert.NotEqual(HeatMapColor.White, render.Pixels[0, 1]);
    }

    [Fact]
    public void ProbeAtPixel_OutsideGrid_ReturnsNull()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new float[,] { { 0.1, 0.2 }, { 0.3, 0.4 } });

        var result = sut.ProbeAtPixel(-1, 0);

        Assert.Null(result);
    }

    [Fact]
    public void BuildRenderData_WithRadialMeshCount20_ReturnsTwentyCircles()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new float[,] { { 0.1, 0.2 }, { 0.3, 0.4 } });

        var render = sut.BuildRenderData(radialMeshCount: 20);

        Assert.Equal(20, render.PolarMesh.CircleRadiiPixels.Count);
    }


    [Fact]
    public void BuildRenderData_WithThirtyDegreeMesh_ReturnsTwelveAngleLines()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new float[,] { { 0.1, 0.2 }, { 0.3, 0.4 } });

        var render = sut.BuildRenderData(angleMeshStepDegrees: 30);

        Assert.Equal(12, render.PolarMesh.AngleLinesDegrees.Count);
    }

    [Fact]
    public void FindMaximumPoint_ReturnsGridMaximum()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new float[,] { { 0.2f, 0.8f }, { 0.3f, 0.4f } });

        var max = sut.FindMaximumPoint();

        Assert.Equal(0, max.Row);
        Assert.Equal(1, max.Column);
        Assert.Equal(0.8f, max.Value, 6);
    }

    [Fact]
    public void FindMaxSumRectangle_ReturnsBestRectanglePosition()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new float[,]
        {
            { 1f, 1f, 1f },
            { 1f, 5f, 5f },
            { 1f, 5f, 5f }
        });

        var rect = sut.FindMaxSumRectangle(rectangleHeight: 2, rectangleWidth: 2);

        Assert.Equal(1, rect.TopRow);
        Assert.Equal(1, rect.LeftColumn);
        Assert.Equal(20f, rect.Sum, 6);
    }

}
