namespace PolarToCartesianInterpolator;

public static class ExampleUsage
{
    public static double[,] Create400By400SampleGrid()
    {
        var rings = new List<RadiusValues>
        {
            new(
                5,
                new[] { 0d, 60d, 120d, 180d, 240d, 300d },
                new[] { 0.10d, 0.20d, 0.30d, 0.40d, 0.50d, 0.60d }),
            new(
                7,
                new[] { 0d, 45d, 90d, 135d, 180d, 225d, 270d, 315d },
                new[] { 0.15d, 0.25d, 0.35d, 0.45d, 0.55d, 0.65d, 0.75d, 0.85d }),
            new(
                9,
                new[] { 0d, 45d, 90d, 135d, 180d, 225d, 270d, 315d },
                new[] { 0.20d, 0.30d, 0.40d, 0.50d, 0.60d, 0.70d, 0.80d, 0.90d }),
            new(
                11,
                new[] { 0d, 30d, 60d, 90d, 120d, 150d, 180d, 210d, 240d, 270d, 300d, 330d },
                new[] { 0.21d, 0.31d, 0.41d, 0.51d, 0.61d, 0.71d, 0.81d, 0.78d, 0.68d, 0.58d, 0.48d, 0.38d }),
            new(
                13,
                new[] { 0d, 30d, 60d, 90d, 120d, 150d, 180d, 210d, 240d, 270d, 300d, 330d },
                new[] { 0.23d, 0.33d, 0.43d, 0.53d, 0.63d, 0.73d, 0.83d, 0.80d, 0.70d, 0.60d, 0.50d, 0.40d })
        };

        var interpolator = new PolarGridInterpolator(rings);
        return interpolator.BuildCartesianTemperatureGrid(400);
    }
}

public static class HeatMapExampleUsage
{
    public static HeatMapRenderData CreateHeatMapRenderData(double cutoff = 0.1)
    {
        var grid = ExampleUsage.Create400By400SampleGrid();
        var control = new CartesianHeatMapControl(cutoff);
        control.SetGrid(grid);
        return control.BuildRenderData();
    }

    public static HeatMapProbeResult? ProbeSamplePoint(double pixelX, double pixelY, double cutoff = 0.1)
    {
        var grid = ExampleUsage.Create400By400SampleGrid();
        var control = new CartesianHeatMapControl(cutoff);
        control.SetGrid(grid);
        return control.ProbeAtPixel(pixelX, pixelY);
    }
}
