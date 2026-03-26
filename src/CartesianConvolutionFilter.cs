namespace PolarToCartesianInterpolator;

public static class CartesianConvolutionFilter
{
    public static float[,] CreateBivariateNormalKernel(int length, double sigmaX, double sigmaY, double meanX, double meanY)
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

        var kernel = new float[length, length];
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
                kernel[y, x] = (float)value;
                sum += value;
            }
        }

        if (sum == 0d)
            throw new InvalidOperationException("Gaussian kernel normalize edilemedi (toplam sifir).");

        for (var y = 0; y < length; y++)
        {
            for (var x = 0; x < length; x++)
            {
                kernel[y, x] = (float)(kernel[y, x] / sum);
            }
        }

        return kernel;
    }

    /// <summary>
    /// Simplified symmetric Gaussian kernel üretir: meanX = meanY = 0 ve sigmaX = sigmaY = r.
    /// </summary>
    public static float[,] CreateNormalKernel(int length, double r)
    {
        if (length <= 0 || length % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Kernel boyutu sifirdan buyuk tek sayi olmali.");
        if (!double.IsFinite(r) || r <= 0)
            throw new ArgumentOutOfRangeException(nameof(r), "r (sigma) sifirdan buyuk olmali.");

        return CreateBivariateNormalKernel(length, r, r, 0d, 0d);
    }

    public static float[] CreateSeparableNormalKernel1D(int length, double r)
    {
        if (length <= 0 || length % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Kernel boyutu sifirdan buyuk tek sayi olmali.");
        if (!double.IsFinite(r) || r <= 0)
            throw new ArgumentOutOfRangeException(nameof(r), "r (sigma) sifirdan buyuk olmali.");

        var kernel = new float[length];
        var radius = length / 2;
        var sum = 0d;

        for (var i = 0; i < length; i++)
        {
            var x = i - radius;
            var value = Math.Exp(-0.5 * ((x * x) / (r * r)));
            kernel[i] = (float)value;
            sum += value;
        }

        for (var i = 0; i < length; i++)
        {
            kernel[i] = (float)(kernel[i] / sum);
        }

        return kernel;
    }

    public static float[,] ApplySingleThread(float[,] input, float[,] kernel)
    {
        ValidateInputAndKernel(input, kernel, out var height, out var width, out var kernelHeight, out _);
        var radius = kernelHeight / 2;
        var padded = CreateReplicatePadded(input, radius, radius);
        var output = new float[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var ky = 0; ky < kernelHeight; ky++)
                {
                    var sourceY = y + ky;
                    for (var kx = 0; kx < kernelHeight; kx++)
                    {
                        sum += padded[sourceY, x + kx] * kernel[ky, kx];
                    }
                }

                output[y, x] = sum;
            }
        }

        return output;
    }

    public static float[,] ApplyParallel(float[,] input, float[,] kernel)
    {
        ValidateInputAndKernel(input, kernel, out var height, out var width, out var kernelHeight, out _);
        var radius = kernelHeight / 2;
        var padded = CreateReplicatePadded(input, radius, radius);
        var output = new float[height, width];

        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var ky = 0; ky < kernelHeight; ky++)
                {
                    var sourceY = y + ky;
                    for (var kx = 0; kx < kernelHeight; kx++)
                    {
                        sum += padded[sourceY, x + kx] * kernel[ky, kx];
                    }
                }

                output[y, x] = sum;
            }
        });

        return output;
    }

    public static float[,] ApplySeparableSingleThread(float[,] input, float[] kernel1D)
    {
        ValidateInputAndKernel1D(input, kernel1D, out var height, out var width, out var kernelLength);
        var radius = kernelLength / 2;

        var paddedRows = CreateReplicatePadded(input, 0, radius);
        var temp = new float[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = 0; k < kernelLength; k++)
                {
                    sum += paddedRows[y, x + k] * kernel1D[k];
                }

                temp[y, x] = sum;
            }
        }

        var paddedCols = CreateReplicatePadded(temp, radius, 0);
        var output = new float[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = 0; k < kernelLength; k++)
                {
                    sum += paddedCols[y + k, x] * kernel1D[k];
                }

                output[y, x] = sum;
            }
        }

        return output;
    }

    public static float[,] ApplySeparableParallel(float[,] input, float[] kernel1D)
    {
        ValidateInputAndKernel1D(input, kernel1D, out var height, out var width, out var kernelLength);
        var radius = kernelLength / 2;

        var paddedRows = CreateReplicatePadded(input, 0, radius);
        var temp = new float[height, width];
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = 0; k < kernelLength; k++)
                {
                    sum += paddedRows[y, x + k] * kernel1D[k];
                }

                temp[y, x] = sum;
            }
        });

        var paddedCols = CreateReplicatePadded(temp, radius, 0);
        var output = new float[height, width];
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = 0; k < kernelLength; k++)
                {
                    sum += paddedCols[y + k, x] * kernel1D[k];
                }

                output[y, x] = sum;
            }
        });

        return output;
    }

    public static float[,] Apply(float[,] input, float[,] kernel)
    {
        return ApplySingleThread(input, kernel);
    }

    private static void ValidateInputAndKernel(
        float[,] input,
        float[,] kernel,
        out int height,
        out int width,
        out int kernelHeight,
        out int kernelWidth)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));

        height = input.GetLength(0);
        width = input.GetLength(1);
        kernelHeight = kernel.GetLength(0);
        kernelWidth = kernel.GetLength(1);

        if (height == 0 || width == 0) throw new ArgumentException("Input grid boş olamaz.", nameof(input));
        if (kernelHeight == 0 || kernelWidth == 0) throw new ArgumentException("Kernel boş olamaz.", nameof(kernel));
        if (kernelHeight != kernelWidth) throw new ArgumentException("Kernel kare (square) olmalıdır.", nameof(kernel));
        if (kernelHeight % 2 == 0) throw new ArgumentException("Kernel boyutu tek sayı olmalıdır (3x3, 5x5 gibi).", nameof(kernel));

    }

    private static void ValidateInputAndKernel1D(float[,] input, float[] kernel1D, out int height, out int width, out int kernelLength)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (kernel1D is null) throw new ArgumentNullException(nameof(kernel1D));

        height = input.GetLength(0);
        width = input.GetLength(1);
        kernelLength = kernel1D.Length;
        if (height == 0 || width == 0) throw new ArgumentException("Input grid boş olamaz.", nameof(input));
        if (kernelLength == 0) throw new ArgumentException("Kernel boş olamaz.", nameof(kernel1D));
        if (kernelLength % 2 == 0) throw new ArgumentException("Kernel boyutu tek sayı olmalıdır (3, 5 gibi).", nameof(kernel1D));
    }

    private static float[,] CreateReplicatePadded(float[,] input, int padY, int padX)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var padded = new float[height + (2 * padY), width + (2 * padX)];

        for (var y = 0; y < padded.GetLength(0); y++)
        {
            var sourceY = Clamp(y - padY, 0, height - 1);
            for (var x = 0; x < padded.GetLength(1); x++)
            {
                var sourceX = Clamp(x - padX, 0, width - 1);
                padded[y, x] = input[sourceY, sourceX];
            }
        }

        return padded;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
}
