namespace PolarToCartesianInterpolator;

public sealed class RadiusValues
{
    public RadiusValues(double radius, IReadOnlyList<double> anglesDegrees, IReadOnlyList<double> temperatures)
    {
        if (anglesDegrees is null) throw new ArgumentNullException(nameof(anglesDegrees));
        if (temperatures is null) throw new ArgumentNullException(nameof(temperatures));
        if (anglesDegrees.Count < 2)
            throw new ArgumentException("Her radius için en az 2 açı değeri olmalıdır.", nameof(anglesDegrees));
        if (anglesDegrees.Count != temperatures.Count)
            throw new ArgumentException("Açı ve sıcaklık listeleri aynı uzunlukta olmalıdır.");

        Radius = radius;
        AngleStepDegrees = 360.0 / anglesDegrees.Count;

        for (var i = 0; i < anglesDegrees.Count; i++)
        {
            var expected = i * AngleStepDegrees;
            if (Math.Abs(anglesDegrees[i] - expected) > 1e-9)
                throw new ArgumentException("Her radius içindeki açılar 0'dan başlayıp eşit aralıklı olmalıdır.", nameof(anglesDegrees));
        }

        if (temperatures.Any(t => t is < 0 or > 1))
            throw new ArgumentOutOfRangeException(nameof(temperatures), "Sıcaklık değerleri 0 ile 1 arasında olmalıdır.");

        AnglesDegrees = anglesDegrees;
        Temperatures = temperatures;
    }

    public double Radius { get; }
    public IReadOnlyList<double> AnglesDegrees { get; }
    public IReadOnlyList<double> Temperatures { get; }
    public double AngleStepDegrees { get; }
}
