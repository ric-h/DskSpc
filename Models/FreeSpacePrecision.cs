using System.Globalization;

namespace DskSpc.Models;

public static class FreeSpacePrecision
{
    private const double HighPrecisionThresholdGb = 6d;

    public static double ToGb(long bytes)
    {
        return bytes / 1024d / 1024d / 1024d;
    }

    public static string FormatGb(long bytes)
    {
        double freeGb = ToGb(bytes);
        string format = freeGb >= HighPrecisionThresholdGb ? "0.0" : "0.00";

        return freeGb.ToString(format, CultureInfo.InvariantCulture);
    }

    // Scales the free space so comparisons match the same decimal precision shown to the user.
    public static long ToComparableUnits(long bytes)
    {
        double freeGb = ToGb(bytes);
        decimal scale = freeGb >= HighPrecisionThresholdGb ? 10m : 100m;
        decimal preciseGb = (decimal)bytes / (1024m * 1024m * 1024m);

        return decimal.ToInt64(decimal.Round(preciseGb * scale, 0, MidpointRounding.ToEven));
    }
}
