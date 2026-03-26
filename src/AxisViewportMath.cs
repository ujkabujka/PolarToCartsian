namespace PolarToCartesianInterpolator;

public static class AxisViewportMath
{
    public static double MapContentXToCartesian(double drawLeft, double drawWidth, double halfRange, double contentX)
    {
        if (drawWidth <= 0)
            return 0;

        var t = (contentX - drawLeft) / drawWidth;
        return -halfRange + (2 * halfRange * t);
    }

    public static double MapContentYToCartesian(double drawTop, double drawHeight, double halfRange, double contentY)
    {
        if (drawHeight <= 0)
            return 0;

        var t = (contentY - drawTop) / drawHeight;
        return halfRange - (2 * halfRange * t);
    }

    public static double Interpolate(double start, double end, double t)
    {
        return start + ((end - start) * t);
    }
}
