namespace PolarToCartesianInterpolator;

public static class Program
{
    public static void Main()
    {
        var result = InterpolationScenarioTest.Run();

        Console.WriteLine("=== Interpolation Benchmark ===");
        Console.WriteLine($"Ring count: {result.Interpolation.RingCount}");
        Console.WriteLine($"Grid size: {result.Interpolation.GridSize}x{result.Interpolation.GridSize}");
        Console.WriteLine($"Interpolation elapsed: {result.Interpolation.ElapsedMilliseconds:F2} ms");
        Console.WriteLine($"Interpolation center sample: {result.Interpolation.SampleCenterTemperature:F6}");
        Console.WriteLine($"Interpolation corner sample: {result.Interpolation.SampleCornerTemperature:F6}");

        Console.WriteLine();
        Console.WriteLine("=== Convolution Benchmark ===");
        Console.WriteLine($"Single-thread 2D elapsed: {result.ConvolutionBenchmark.SingleThread2DElapsedMilliseconds:F2} ms");
        Console.WriteLine($"Parallel 2D elapsed: {result.ConvolutionBenchmark.Parallel2DElapsedMilliseconds:F2} ms");
        Console.WriteLine($"Single-thread separable elapsed: {result.ConvolutionBenchmark.SingleThreadSeparableElapsedMilliseconds:F2} ms");
        Console.WriteLine($"Parallel separable elapsed: {result.ConvolutionBenchmark.ParallelSeparableElapsedMilliseconds:F2} ms");
        Console.WriteLine($"Filtered center sample: {result.FilteredCenterTemperature:F6}");
        Console.WriteLine($"Filtered corner sample: {result.FilteredCornerTemperature:F6}");

        Console.WriteLine();
        Console.WriteLine("Scenario test completed successfully.");
    }
}
