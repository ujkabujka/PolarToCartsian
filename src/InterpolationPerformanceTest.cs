using System.Diagnostics;

namespace PolarToCartesianInterpolator;

public static class InterpolationPerformanceTest
{
    public static PerformanceResult Run400x400Benchmark()
    {
        var rings = BuildSyntheticRings(
            startRadius: 2,
            endRadiusInclusive: 300,
            radiusStep: 2,
            minimumAngleCount: 60);

        var interpolator = new PolarGridInterpolator(rings);

        var stopwatch = Stopwatch.StartNew();
        var grid = interpolator.BuildCartesianTemperatureGrid(400);
        stopwatch.Stop();

        return new PerformanceResult(
            rings.Count,
            400,
            stopwatch.Elapsed.TotalMilliseconds,
            grid[200, 200],
            grid[0, 0]);
    }

    public static List<RadiusValues> BuildSyntheticRings(
        int startRadius,
        int endRadiusInclusive,
        int radiusStep,
        int minimumAngleCount)
    {
        if (startRadius <= 0) throw new ArgumentOutOfRangeException(nameof(startRadius));
        if (endRadiusInclusive <= startRadius) throw new ArgumentOutOfRangeException(nameof(endRadiusInclusive));
        if (radiusStep <= 0) throw new ArgumentOutOfRangeException(nameof(radiusStep));
        if (minimumAngleCount < 2) throw new ArgumentOutOfRangeException(nameof(minimumAngleCount));

        var rings = new List<RadiusValues>();

        for (var radius = startRadius; radius <= endRadiusInclusive; radius += radiusStep)
        {
            var angleCount = minimumAngleCount + ((radius / radiusStep) % 4) * 12;
            var angleStep = 360.0 / angleCount;

            var angles = new double[angleCount];
            var temperatures = new double[angleCount];

            for (var i = 0; i < angleCount; i++)
            {
                var thetaDegrees = i * angleStep;
                angles[i] = thetaDegrees;

                var normalizedRadius = (radius - startRadius) / (double)(endRadiusInclusive - startRadius);
                var normalizedAngle = thetaDegrees / 360.0;

                var temperature = 0.65 * normalizedRadius + 0.35 * normalizedAngle;
                temperatures[i] = Math.Clamp(temperature, 0.0, 1.0);
            }

            rings.Add(new RadiusValues(radius, angles, temperatures));
        }

        return rings;
    }
}

public readonly record struct PerformanceResult(
    int RingCount,
    int GridSize,
    double ElapsedMilliseconds,
    double SampleCenterTemperature,
    double SampleCornerTemperature);
