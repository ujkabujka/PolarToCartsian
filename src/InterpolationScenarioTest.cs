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

        var kernel2D = CartesianConvolutionFilter.CreateNormalKernel(length: 9, r: 1.8);
        var kernel1D = CartesianConvolutionFilter.CreateSeparableNormalKernel1D(length: 9, r: 1.8);

        var single2D = RunAndMeasure(() => CartesianConvolutionFilter.ApplySingleThread(cartesianGrid, kernel2D));
        var parallel2D = RunAndMeasure(() => CartesianConvolutionFilter.ApplyParallel(cartesianGrid, kernel2D));
        var singleSeparable = RunAndMeasure(() => CartesianConvolutionFilter.ApplySeparableSingleThread(cartesianGrid, kernel1D));
        var parallelSeparable = RunAndMeasure(() => CartesianConvolutionFilter.ApplySeparableParallel(cartesianGrid, kernel1D));

        var filteredGrid = parallelSeparable.Grid;

        ValidateGrid(filteredGrid, 400);

        return new ScenarioTestResult(
            interpolationResult,
            new ConvolutionBenchmarkResult(
                single2D.ElapsedMilliseconds,
                parallel2D.ElapsedMilliseconds,
                singleSeparable.ElapsedMilliseconds,
                parallelSeparable.ElapsedMilliseconds),
            filteredGrid[200, 200],
            filteredGrid[0, 0]);
    }

    private static (float[,] Grid, double ElapsedMilliseconds) RunAndMeasure(Func<float[,]> operation)
    {
        var watch = Stopwatch.StartNew();
        var grid = operation();
        watch.Stop();
        return (grid, watch.Elapsed.TotalMilliseconds);
    }

    private static void ReplaceInvalidValuesWithZero(float[,] grid)
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

    private static void ValidateGrid(float[,] grid, int expectedSize)
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
    ConvolutionBenchmarkResult ConvolutionBenchmark,
    double FilteredCenterTemperature,
    double FilteredCornerTemperature);

public readonly record struct ConvolutionBenchmarkResult(
    double SingleThread2DElapsedMilliseconds,
    double Parallel2DElapsedMilliseconds,
    double SingleThreadSeparableElapsedMilliseconds,
    double ParallelSeparableElapsedMilliseconds);
