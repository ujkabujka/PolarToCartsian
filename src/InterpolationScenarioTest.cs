using System.Diagnostics;

namespace PolarToCartesianInterpolator;

public static class InterpolationScenarioTest
{
    public static ScenarioTestResult Run()
    {
        var interpolationResult = InterpolationPerformanceTest.Run400x400Benchmark();
        var rings = InterpolationPerformanceTest.BuildSyntheticRings(2, 300, 2, 60);
        var interpolator = new PolarGridInterpolator(rings);

        var cartesianGrid = interpolator.BuildCartesianTemperatureGrid(400);
        ReplaceInvalidValuesWithZero(cartesianGrid);

        var kernel = new double[,]
        {
            { 1d / 16d, 2d / 16d, 1d / 16d },
            { 2d / 16d, 4d / 16d, 2d / 16d },
            { 1d / 16d, 2d / 16d, 1d / 16d }
        };

        var convolutionWatch = Stopwatch.StartNew();
        var filteredGrid = CartesianConvolutionFilter.Apply(cartesianGrid, kernel);
        convolutionWatch.Stop();

        ValidateGrid(filteredGrid, 400);

        return new ScenarioTestResult(
            interpolationResult,
            convolutionWatch.Elapsed.TotalMilliseconds,
            filteredGrid[200, 200],
            filteredGrid[0, 0]);
    }

    private static void ReplaceInvalidValuesWithZero(double[,] grid)
    {
        for (var row = 0; row < grid.GetLength(0); row++)
        {
            for (var col = 0; col < grid.GetLength(1); col++)
            {
                if (double.IsNaN(grid[row, col]) || double.IsInfinity(grid[row, col]))
                    grid[row, col] = 0;
            }
        }
    }

    private static void ValidateGrid(double[,] grid, int expectedSize)
    {
        if (grid.GetLength(0) != expectedSize || grid.GetLength(1) != expectedSize)
            throw new InvalidOperationException("Grid boyutu beklenen 400x400 değil.");

        for (var row = 0; row < grid.GetLength(0); row++)
        {
            for (var col = 0; col < grid.GetLength(1); col++)
            {
                var value = grid[row, col];
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new InvalidOperationException("Grid içerisinde geçersiz (NaN/Infinity) değer bulundu.");
            }
        }
    }
}

public readonly record struct ScenarioTestResult(
    PerformanceResult Interpolation,
    double ConvolutionElapsedMilliseconds,
    double FilteredCenterTemperature,
    double FilteredCornerTemperature);
