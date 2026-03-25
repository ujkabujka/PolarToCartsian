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
    /// <summary>
    /// Belirtilen boyut ve dağılım parametreleriyle iki değişkenli normal dağılım tabanlı bir konvolüsyon çekirdeği oluşturur.
    /// Dönen çekirdek, tüm elemanlarının toplamı 1.0 olacak şekilde normalize edilir.
    /// </summary>
    /// <param name="length">Kare çekirdeğin kenar uzunluğu. Pozitif ve tek sayı olmalıdır.</param>
    /// <param name="sigmaX">X ekseni standart sapması. Sıfırdan büyük olmalıdır.</param>
    /// <param name="sigmaY">Y ekseni standart sapması. Sıfırdan büyük olmalıdır.</param>
    /// <param name="meanX">X ekseni ortalaması (çekirdek merkezine göre).</param>
    /// <param name="meanY">Y ekseni ortalaması (çekirdek merkezine göre).</param>
    /// <returns>Toplamı 1.0 olacak şekilde normalize edilmiş iki boyutlu normal dağılım çekirdeği.</returns>
    public static double[,] CreateBivariateNormalKernel(int length, double sigmaX, double sigmaY, double meanX, double meanY)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Kernel boyutu pozitif olmalıdır.");
        if (length % 2 == 0) throw new ArgumentException("Kernel boyutu tek sayı olmalıdır (3x3, 5x5 gibi).", nameof(length));
        if (sigmaX <= 0) throw new ArgumentOutOfRangeException(nameof(sigmaX), "sigmaX sıfırdan büyük olmalıdır.");
        if (sigmaY <= 0) throw new ArgumentOutOfRangeException(nameof(sigmaY), "sigmaY sıfırdan büyük olmalıdır.");

        var kernel = new double[length, length];
        var center = length / 2;
        var coefficient = 1d / (2d * Math.PI * sigmaX * sigmaY);
        var sigmaXSquared = sigmaX * sigmaX;
        var sigmaYSquared = sigmaY * sigmaY;
        var sum = 0d;

        for (var row = 0; row < length; row++)
        {
            var y = row - center;

            for (var col = 0; col < length; col++)
            {
                var x = col - center;
                var exponent = -0.5d * (((x - meanX) * (x - meanX) / sigmaXSquared) + ((y - meanY) * (y - meanY) / sigmaYSquared));
                var value = coefficient * Math.Exp(exponent);
                kernel[row, col] = value;
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
        for (var row = 0; row < length; row++)
        {
            for (var col = 0; col < length; col++)
            {
                kernel[row, col] /= sum;
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
