namespace DskSpc.Models;

public sealed record DriveSnapshot(
    DateTime Timestamp,
    string Drive,
    long UsedBytes,
    long FreeBytes,
    long TotalBytes)
{
    public double UsedPercent => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes * 100;

    public double FreePercent => TotalBytes == 0 ? 0 : (double)FreeBytes / TotalBytes * 100;
}