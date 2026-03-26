namespace PolarToCartesianInterpolator;

public sealed class PolarGridInterpolator
{
    private readonly RadiusValues[] _rings;
    private readonly double _firstRadius;
    private readonly double _radiusStep;

    public PolarGridInterpolator(IReadOnlyList<RadiusValues> rings)
    {
        if (rings is null) throw new ArgumentNullException(nameof(rings));
        if (rings.Count < 2) throw new ArgumentException("Interpolasyon için en az 2 radius halkası gerekir.", nameof(rings));

        _rings = rings.OrderBy(r => r.Radius).ToArray();
        _firstRadius = _rings[0].Radius;
        _radiusStep = _rings[1].Radius - _rings[0].Radius;

        if (_radiusStep <= 0)
            throw new ArgumentException("Radius değerleri artan olmalı.", nameof(rings));

        const double tolerance = 1e-9;
        for (var i = 1; i < _rings.Length; i++)
        {
            var expected = _firstRadius + (i * _radiusStep);
            if (Math.Abs(_rings[i].Radius - expected) > tolerance)
                throw new ArgumentException("Tüm radius halkaları sabit artışta olmalıdır.", nameof(rings));
        }
    }

    public double InterpolateTemperatureAtCartesian(double x, double y)
    {
        var radius = Math.Sqrt((x * x) + (y * y));
        var theta = NormalizeDegrees(Math.Atan2(y, x) * (180.0 / Math.PI));
        return InterpolateTemperaturePolar(radius, theta);
    }

    public double[,] BuildCartesianTemperatureGrid(int sizeMeters)
    {
        if (sizeMeters <= 0) throw new ArgumentOutOfRangeException(nameof(sizeMeters));

        var grid = new double[sizeMeters, sizeMeters];
        var half = sizeMeters / 2.0;

        for (var row = 0; row < sizeMeters; row++)
        {
            var y = half - row;
            for (var col = 0; col < sizeMeters; col++)
            {
                var x = col - half;
                grid[row, col] = InterpolateTemperatureAtCartesian(x, y);
            }
        }

        return grid;
    }

    public double InterpolateTemperaturePolar(double radius, double thetaDegrees)
    {
        if (radius < _firstRadius)  
            radius = _firstRadius;
        else if (radius > _rings[^1].Radius)
        {
            return 0;
        }
        var lowerIndex = (int)Math.Floor((radius - _firstRadius) / _radiusStep);
        if (lowerIndex >= _rings.Length - 1)
            lowerIndex = _rings.Length - 2;

        var upperIndex = lowerIndex + 1;
        var lowerRing = _rings[lowerIndex];
        var upperRing = _rings[upperIndex];

        var radialRatio = (radius - lowerRing.Radius) / _radiusStep;

        var lowerTempOnTheta = InterpolateOnRing(lowerRing, thetaDegrees);
        var upperTempOnTheta = InterpolateOnRing(upperRing, thetaDegrees);

        return Lerp(lowerTempOnTheta, upperTempOnTheta, radialRatio);
    }

    private static double InterpolateOnRing(RadiusValues ring, double thetaDegrees)
    {
        var theta = NormalizeDegrees(thetaDegrees);
        var n = ring.AnglesDegrees.Count;
        var step = ring.AngleStepDegrees;

        var leftIndex = (int)Math.Floor(theta / step);
        if (leftIndex >= n)
            leftIndex = n - 1;

        var rightIndex = (leftIndex + 1) % n;

        var leftAngle = leftIndex * step;
        var rightAngle = rightIndex == 0 ? 360.0 : rightIndex * step;

        var segmentLength = rightAngle - leftAngle;
        var angleRatio = segmentLength <= 0 ? 0 : (theta - leftAngle) / segmentLength;

        var leftTemp = ring.Temperatures[leftIndex];
        var rightTemp = ring.Temperatures[rightIndex];

        return Lerp(leftTemp, rightTemp, angleRatio);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double NormalizeDegrees(double theta)
    {
        var normalized = theta % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
