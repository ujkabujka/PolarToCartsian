namespace PolarToCartesianInterpolator;

public static class CartesianConvolutionFilter
{
    public static double[,] Apply(double[,] input, double[,] kernel)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));

        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var kernelHeight = kernel.GetLength(0);
        var kernelWidth = kernel.GetLength(1);

        if (height == 0 || width == 0) throw new ArgumentException("Input grid boş olamaz.", nameof(input));
        if (kernelHeight == 0 || kernelWidth == 0) throw new ArgumentException("Kernel boş olamaz.", nameof(kernel));
        if (kernelHeight != kernelWidth) throw new ArgumentException("Kernel kare (square) olmalıdır.", nameof(kernel));
        if (kernelHeight % 2 == 0) throw new ArgumentException("Kernel boyutu tek sayı olmalıdır (3x3, 5x5 gibi).", nameof(kernel));

        var output = new double[height, width];
        var radius = kernelHeight / 2;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0d;

                for (var ky = -radius; ky <= radius; ky++)
                {
                    var sourceY = Clamp(y + ky, 0, height - 1);
                    var kernelY = ky + radius;

                    for (var kx = -radius; kx <= radius; kx++)
                    {
                        var sourceX = Clamp(x + kx, 0, width - 1);
                        var kernelX = kx + radius;

                        sum += input[sourceY, sourceX] * kernel[kernelY, kernelX];
                    }
                }

                output[y, x] = sum;
            }
        }

        return output;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
