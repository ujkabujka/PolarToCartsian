namespace PolarToCartesianInterpolator;

public sealed class CartesianHeatMapControl
{
    private static readonly ColorStop[] GradientStops =
    [
        new(1.00, new HeatMapColor(255, 0, 0)),    // red
        new(0.75, new HeatMapColor(255, 165, 0)),  // orange
        new(0.50, new HeatMapColor(255, 255, 0)),  // yellow
        new(0.25, new HeatMapColor(0, 128, 0)),    // green
        new(0.00, new HeatMapColor(0, 0, 255))     // blue
    ];

    private double[,] _grid = new double[0, 0];
    private int _gridWidth;
    private int _gridHeight;
    private double _cutoff = 0.1;

    public CartesianHeatMapControl(double cutoff = 0.1)
    {
        Cutoff = cutoff;
    }

    public double Cutoff
    {
        get => _cutoff;
        set
        {
            if (value is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Cutoff 0 ile 1 arasında olmalıdır.");

            _cutoff = value;
        }
    }

    public void SetGrid(double[,] grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        _grid = grid;
        _gridHeight = grid.GetLength(0);
        _gridWidth = grid.GetLength(1);

        if (_gridHeight == 0 || _gridWidth == 0)
            throw new ArgumentException("Grid boş olamaz.", nameof(grid));
    }

    public HeatMapRenderData BuildRenderData(int legendStepCount = 5, int radialMeshCount = 5, int angleMeshStepDegrees = 30)
    {
        EnsureGridIsReady();

        var pixels = new HeatMapColor[_gridHeight, _gridWidth];
        for (var row = 0; row < _gridHeight; row++)
        {
            for (var col = 0; col < _gridWidth; col++)
            {
                pixels[row, col] = MapTemperatureToColor(_grid[row, col]);
            }
        }

        var xAxis = BuildAxis(_gridWidth, isXAxis: true);
        var yAxis = BuildAxis(_gridHeight, isXAxis: false);
        var mesh = BuildPolarMesh(radialMeshCount, angleMeshStepDegrees);
        var legend = BuildLegend(legendStepCount);

        return new HeatMapRenderData(pixels, xAxis, yAxis, mesh, legend, Cutoff);
    }

    public HeatMapProbeResult? ProbeAtPixel(double pixelX, double pixelY)
    {
        EnsureGridIsReady();

        if (pixelX < 0 || pixelY < 0 || pixelX > _gridWidth - 1 || pixelY > _gridHeight - 1)
            return null;

        var xCartesian = PixelToCartesianX(pixelX);
        var yCartesian = PixelToCartesianY(pixelY);
        var radius = Math.Sqrt((xCartesian * xCartesian) + (yCartesian * yCartesian));

        var angle = Math.Atan2(yCartesian, xCartesian) * (180.0 / Math.PI);
        if (angle < 0)
            angle += 360;

        var temperature = BilinearInterpolateTemperature(pixelX, pixelY);
        var color = MapTemperatureToColor(temperature);

        return new HeatMapProbeResult(
            pixelX,
            pixelY,
            xCartesian,
            yCartesian,
            radius,
            angle,
            temperature,
            color,
            isBelowCutoff: !double.IsNaN(temperature) && temperature < Cutoff);
    }

    public IReadOnlyList<LegendStop> BuildLegend(int stepCount = 5)
    {
        if (stepCount < 2)
            throw new ArgumentOutOfRangeException(nameof(stepCount), "Legend için en az 2 adım gerekir.");

        var stops = new List<LegendStop>(stepCount);
        for (var i = 0; i < stepCount; i++)
        {
            var t = i / (double)(stepCount - 1);
            var value = 1.0 - t;
            var color = MapTemperatureToColor(value);
            stops.Add(new LegendStop(value, color));
        }

        return stops;
    }

    private PolarMesh BuildPolarMesh(int radialMeshCount, int angleMeshStepDegrees)
    {
        if (radialMeshCount < 1)
            throw new ArgumentOutOfRangeException(nameof(radialMeshCount));
        if (angleMeshStepDegrees <= 0 || angleMeshStepDegrees > 360)
            throw new ArgumentOutOfRangeException(nameof(angleMeshStepDegrees));

        var maxRadius = Math.Min(_gridWidth, _gridHeight) / 2.0;
        var circles = new List<double>(radialMeshCount);
        for (var i = 1; i <= radialMeshCount; i++)
        {
            circles.Add(maxRadius * (i / (double)radialMeshCount));
        }

        var angleLines = new List<double>();
        for (var angle = 0; angle < 360; angle += angleMeshStepDegrees)
        {
            angleLines.Add(angle);
        }

        return new PolarMesh(circles, angleLines);
    }

    private IReadOnlyList<AxisTick> BuildAxis(int size, bool isXAxis)
    {
        var ticks = new List<AxisTick>();
        if (size <= 0)
            return ticks;

        var half = size / 2.0;
        var step = Math.Max(1, size / 8);

        for (var px = 0; px < size; px += step)
        {
            var value = isXAxis ? px - half : half - px;
            ticks.Add(new AxisTick(px, value));
        }

        if (ticks.Count == 0 || ticks[^1].PixelPosition != size - 1)
        {
            var endValue = isXAxis ? (size - 1) - half : half - (size - 1);
            ticks.Add(new AxisTick(size - 1, endValue));
        }

        return ticks;
    }

    private double BilinearInterpolateTemperature(double pixelX, double pixelY)
    {
        var x0 = (int)Math.Floor(pixelX);
        var y0 = (int)Math.Floor(pixelY);
        var x1 = Math.Min(x0 + 1, _gridWidth - 1);
        var y1 = Math.Min(y0 + 1, _gridHeight - 1);

        var tx = pixelX - x0;
        var ty = pixelY - y0;

        var c00 = _grid[y0, x0];
        var c10 = _grid[y0, x1];
        var c01 = _grid[y1, x0];
        var c11 = _grid[y1, x1];

        if (double.IsNaN(c00) || double.IsNaN(c10) || double.IsNaN(c01) || double.IsNaN(c11))
            return double.NaN;

        var a = Lerp(c00, c10, tx);
        var b = Lerp(c01, c11, tx);
        return Lerp(a, b, ty);
    }

    private HeatMapColor MapTemperatureToColor(double temperature)
    {
        if (double.IsNaN(temperature))
            return HeatMapColor.Transparent;

        var clamped = Math.Clamp(temperature, 0, 1);
        if (clamped < Cutoff)
            return HeatMapColor.White;

        for (var i = 0; i < GradientStops.Length - 1; i++)
        {
            var from = GradientStops[i];
            var to = GradientStops[i + 1];

            if (clamped <= from.Value && clamped >= to.Value)
            {
                var t = (clamped - to.Value) / (from.Value - to.Value);
                return HeatMapColor.Lerp(to.Color, from.Color, t);
            }
        }

        return clamped >= 1 ? GradientStops[0].Color : GradientStops[^1].Color;
    }

    private double PixelToCartesianX(double pixelX) => pixelX - (_gridWidth / 2.0);

    private double PixelToCartesianY(double pixelY) => (_gridHeight / 2.0) - pixelY;

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private void EnsureGridIsReady()
    {
        if (_gridHeight == 0 || _gridWidth == 0)
            throw new InvalidOperationException("Önce SetGrid çağrılmalıdır.");
    }

    private readonly record struct ColorStop(double Value, HeatMapColor Color);
}

public readonly record struct HeatMapColor(byte R, byte G, byte B, byte A = 255)
{
    public static HeatMapColor White => new(255, 255, 255);
    public static HeatMapColor Transparent => new(0, 0, 0, 0);

    public static HeatMapColor Lerp(HeatMapColor from, HeatMapColor to, double t)
    {
        var clamped = Math.Clamp(t, 0.0, 1.0);

        return new HeatMapColor(
            R: (byte)Math.Round(from.R + ((to.R - from.R) * clamped)),
            G: (byte)Math.Round(from.G + ((to.G - from.G) * clamped)),
            B: (byte)Math.Round(from.B + ((to.B - from.B) * clamped)),
            A: (byte)Math.Round(from.A + ((to.A - from.A) * clamped)));
    }
}

public readonly record struct AxisTick(double PixelPosition, double CartesianValue);

public readonly record struct LegendStop(double Value, HeatMapColor Color);

public sealed record PolarMesh(
    IReadOnlyList<double> CircleRadiiPixels,
    IReadOnlyList<double> AngleLinesDegrees);

public sealed record HeatMapRenderData(
    HeatMapColor[,] Pixels,
    IReadOnlyList<AxisTick> XAxisTicks,
    IReadOnlyList<AxisTick> YAxisTicks,
    PolarMesh PolarMesh,
    IReadOnlyList<LegendStop> Legend,
    double Cutoff);

public readonly record struct HeatMapProbeResult(
    double PixelX,
    double PixelY,
    double CartesianX,
    double CartesianY,
    double Radius,
    double AngleDegrees,
    double InterpolatedTemperature,
    HeatMapColor Color,
    bool IsBelowCutoff);
