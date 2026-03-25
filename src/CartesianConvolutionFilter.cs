namespace PolarToCartesianInterpolator;

public static class CartesianConvolutionFilter
{
public static double[,] CreateBivariateNormalKernel(int length, double sigmaX, double sigmaY, double meanX, double meanY)
    {
        if (length <= 0 || length % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Kernel boyutu sifirdan buyuk tek sayi olmali.");
        if (!double.IsFinite(sigmaX) || sigmaX <= 0)
            throw new ArgumentOutOfRangeException(nameof(sigmaX), "SigmaX sifirdan buyuk olmali.");
        if (!double.IsFinite(sigmaY) || sigmaY <= 0)
            throw new ArgumentOutOfRangeException(nameof(sigmaY), "SigmaY sifirdan buyuk olmali.");
        if (!double.IsFinite(meanX))
            throw new ArgumentOutOfRangeException(nameof(meanX), "MeanX sonlu bir sayi olmali.");
        if (!double.IsFinite(meanY))
            throw new ArgumentOutOfRangeException(nameof(meanY), "MeanY sonlu bir sayi olmali.");

        var kernel = new double[length, length];
        var radius = length / 2;
        var sum = 0d;

        for (var y = 0; y < length; y++)
        {
            for (var x = 0; x < length; x++)
            {
                var shiftedX = x - radius - meanX;
                var shiftedY = y - radius - meanY;

                var exponent = -0.5 * (
                    (shiftedX * shiftedX) / (sigmaX * sigmaX) +
                    (shiftedY * shiftedY) / (sigmaY * sigmaY));

                var value = Math.Exp(exponent);
                kernel[y, x] = value;
                sum += value;
            }
        }

        if (sum == 0d)
            throw new InvalidOperationException("Gaussian kernel normalize edilemedi (toplam sifir).");

        for (var y = 0; y < length; y++)
        {
            for (var x = 0; x < length; x++)
            {
                kernel[y, x] /= sum;
            }
        }

        return kernel;
    }

    /// <summary>
    /// Simplified symmetric Gaussian kernel üretir: meanX = meanY = 0 ve sigmaX = sigmaY = r.
    /// </summary>
    public static double[,] CreateNormalKernel(int length, double r)
    {
        if (length <= 0 || length % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Kernel boyutu sifirdan buyuk tek sayi olmali.");
        if (!double.IsFinite(r) || r <= 0)
            throw new ArgumentOutOfRangeException(nameof(r), "r (sigma) sifirdan buyuk olmali.");

        return CreateBivariateNormalKernel(length, r, r, 0d, 0d);
    }


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
