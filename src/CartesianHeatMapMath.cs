namespace PolarToCartesianInterpolator;

public static class CartesianHeatMapMath
{
    public static double Sum(float[,] heatMap)
    {
        var (height, width) = GetDimensionsOrThrow(heatMap, nameof(heatMap));
        double result = 0;

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                result += heatMap[row, col];
            }
        }

        return result;
    }

    public static float[,] SubtractFromOne(float[,] heatMap)
    {
        var (height, width) = GetDimensionsOrThrow(heatMap, nameof(heatMap));
        var result = new float[height, width];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                result[row, col] = (float)(1.0 - heatMap[row, col]);
            }
        }

        return result;
    }

    public static double CalculateTemperatureTimesArea(float[,] heatMap, double cellSize = 1.0)
    {
        var (height, width) = GetDimensionsOrThrow(heatMap, nameof(heatMap));
        ValidateCellGeometry(height, width, cellSize);

        var cellArea = cellSize * cellSize;
        var total = 0d;

        for (var row = 0; row < height - 1; row++)
        {
            for (var col = 0; col < width - 1; col++)
            {
                var topLeft = SanitizeForArea(heatMap[row, col]);
                var topRight = SanitizeForArea(heatMap[row, col + 1]);
                var bottomLeft = SanitizeForArea(heatMap[row + 1, col]);
                var bottomRight = SanitizeForArea(heatMap[row + 1, col + 1]);

                var averageTemperature = (topLeft + topRight + bottomLeft + bottomRight) / 4.0;
                total += cellArea * averageTemperature;
            }
        }

        return total;
    }

    public static float[,] MultiplyElementWise(float[,] leftHeatMap, float[,] rightHeatMap)
    {
        var (leftHeight, leftWidth) = GetDimensionsOrThrow(leftHeatMap, nameof(leftHeatMap));
        var (rightHeight, rightWidth) = GetDimensionsOrThrow(rightHeatMap, nameof(rightHeatMap));

        if (leftHeight != rightHeight || leftWidth != rightWidth)
        {
            throw new ArgumentException(
                "Heat map boyutlari esit olmali.",
                nameof(rightHeatMap));
        }

        var result = new float[leftHeight, leftWidth];

        for (var row = 0; row < leftHeight; row++)
        {
            for (var col = 0; col < leftWidth; col++)
            {
                result[row, col] = leftHeatMap[row, col] * rightHeatMap[row, col];
            }
        }

        return result;
    }

    public static float[,] ApplyBinaryThreshold(float[,] heatMap, double threshold)
    {
        var (height, width) = GetDimensionsOrThrow(heatMap, nameof(heatMap));
        ValidateThreshold(threshold);

        var result = new float[height, width];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var value = heatMap[row, col];
                result[row, col] = double.IsFinite(value) && value >= threshold ? 1f : 0f;
            }
        }

        return result;
    }

    public static double CalculateThresholdedTemperatureTimesArea(float[,] heatMap, double threshold, double cellSize = 1.0)
    {
        ValidateThreshold(threshold);
        var thresholded = ApplyBinaryThreshold(heatMap, threshold);
        return CalculateTemperatureTimesArea(thresholded, cellSize);
    }

    private static (int Height, int Width) GetDimensionsOrThrow(float[,] heatMap, string paramName)
    {
        ArgumentNullException.ThrowIfNull(heatMap, paramName);

        var height = heatMap.GetLength(0);
        var width = heatMap.GetLength(1);

        if (height == 0 || width == 0)
            throw new ArgumentException("Heat map bos olamaz.", paramName);

        return (height, width);
    }

    private static void ValidateCellGeometry(int height, int width, double cellSize)
    {
        if (height < 2 || width < 2)
            throw new ArgumentException("Temperature-times-area icin heat map en az 2x2 olmali.");
        if (!double.IsFinite(cellSize) || cellSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size sifirdan buyuk olmali.");
    }

    private static void ValidateThreshold(double threshold)
    {
        if (threshold is < 0 or > 1 || !double.IsFinite(threshold))
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold 0 ile 1 arasinda olmali.");
    }

    private static double SanitizeForArea(double value) => double.IsFinite(value) ? value : 0.0;
}
