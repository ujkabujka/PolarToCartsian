namespace PolarToCartesianInterpolator;

public sealed class ProbabilityDistributer
{
    private static readonly double[] AllowedProbabilities = [0.95d, 0.99d, 0.999d];

    private double[,] _grid = new double[0, 0];
    private double _r50;
    private double _targetProbability = 0.95d;

    public ProbabilityDistributer(double[,] grid)
    {
        SetGrid(grid);
    }

    public double R50
    {
        get => _r50;
        set
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "R50 sıfır veya pozitif bir değer olmalıdır.");

            _r50 = value;
        }
    }

    public double TargetProbability
    {
        get => _targetProbability;
        set
        {
            if (!IsAllowedProbability(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Target probability sadece 0.95, 0.99 veya 0.999 olabilir.");

            _targetProbability = value;
        }
    }

    public double SigmaR => _r50 <= 0 ? 0 : _r50 / Math.Sqrt(2d * Math.Log(2d));

    public int KernelRadius
    {
        get
        {
            if (_r50 <= 0)
                return 0;

            var sigma = SigmaR;
            if (sigma <= 0)
                return 0;

            var radius = sigma * Math.Sqrt(-2d * Math.Log(1d - _targetProbability));
            if (!double.IsFinite(radius) || radius <= 0)
                return 0;

            return (int)Math.Ceiling(radius);
        }
    }

    public int KernelLength
    {
        get
        {
            var radius = KernelRadius;
            return radius <= 0 ? 0 : (radius * 2) + 1;
        }
    }

    public void SetGrid(double[,] grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (grid.GetLength(0) == 0 || grid.GetLength(1) == 0)
            throw new ArgumentException("Grid boş olamaz.", nameof(grid));

        _grid = grid;
    }

    public double[,] CreateKernel()
    {
        if (KernelLength <= 1 || SigmaR <= 0)
            return new double[1, 1] { { 1d } };

        return CartesianConvolutionFilter.CreateNormalKernel(KernelLength, SigmaR);
    }

    public double[,] BuildFilteredGrid()
    {
        if (KernelLength <= 1 || SigmaR <= 0)
            return (double[,])_grid.Clone();

        var kernel = CreateKernel();
        return CartesianConvolutionFilter.Apply(_grid, kernel);
    }

    private static bool IsAllowedProbability(double value)
    {
        foreach (var allowed in AllowedProbabilities)
        {
            if (Math.Abs(allowed - value) < 1e-12)
                return true;
        }

        return false;
    }
}
