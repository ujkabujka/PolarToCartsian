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
        sut.SetGrid(new double[,] { { 0.10, 0.35 } });

        var render = sut.BuildRenderData();

        Assert.Equal(HeatMapColor.White, render.Pixels[0, 0]);
        Assert.NotEqual(HeatMapColor.White, render.Pixels[0, 1]);
    }

    [Fact]
    public void ProbeAtPixel_OutsideGrid_ReturnsNull()
    {
        var sut = new CartesianHeatMapControl();
        sut.SetGrid(new double[,] { { 0.1, 0.2 }, { 0.3, 0.4 } });

        var result = sut.ProbeAtPixel(-1, 0);

        Assert.Null(result);
    }
}
