namespace PolarToCartesianInterpolator;

public static class CartesianHeatMapMath
{
    public static double[,] SubtractFromOne(double[,] heatMap)
    {
        var (height, width) = GetDimensionsOrThrow(heatMap, nameof(heatMap));
        var result = new double[height, width];

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                result[row, col] = 1.0 - heatMap[row, col];
            }
        }

        return result;
    }

    public static double[,] MultiplyElementWise(double[,] leftHeatMap, double[,] rightHeatMap)
    {
        var (leftHeight, leftWidth) = GetDimensionsOrThrow(leftHeatMap, nameof(leftHeatMap));
        var (rightHeight, rightWidth) = GetDimensionsOrThrow(rightHeatMap, nameof(rightHeatMap));

        if (leftHeight != rightHeight || leftWidth != rightWidth)
        {
            throw new ArgumentException(
                "Heat map boyutlari esit olmali.",
                nameof(rightHeatMap));
        }

        var result = new double[leftHeight, leftWidth];

        for (var row = 0; row < leftHeight; row++)
        {
            for (var col = 0; col < leftWidth; col++)
            {
                result[row, col] = leftHeatMap[row, col] * rightHeatMap[row, col];
            }
        }

        return result;
    }

    private static (int Height, int Width) GetDimensionsOrThrow(double[,] heatMap, string paramName)
    {
        ArgumentNullException.ThrowIfNull(heatMap, paramName);

        var height = heatMap.GetLength(0);
        var width = heatMap.GetLength(1);

        if (height == 0 || width == 0)
            throw new ArgumentException("Heat map bos olamaz.", paramName);

        return (height, width);
    }
}
