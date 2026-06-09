using DskSpc.Models;

namespace DskSpc.Services;

public sealed class DriveService
{
    public DriveReadResult TryReadCurrentDrive()
    {
        try {
            string currentDirectory = Directory.GetCurrentDirectory();
            string? root = Path.GetPathRoot(currentDirectory);

            if (string.IsNullOrWhiteSpace(root)) {
                return DriveReadResult.Failure("Could not determine the drive from the current directory.");
            }

            var drive = new DriveInfo(root);

            if (!drive.IsReady) {
                return DriveReadResult.Failure($"Drive {drive.Name} is not ready.");
            }

            long total = drive.TotalSize;
            long free = drive.TotalFreeSpace;
            long used = total - free;

            var snapshot = new DriveSnapshot(
                Timestamp: DateTime.Now,
                Drive: drive.Name,
                UsedBytes: used,
                FreeBytes: free,
                TotalBytes: total);

            return DriveReadResult.Success(snapshot);
        }
        catch (UnauthorizedAccessException) {
            return DriveReadResult.Failure("No permission to read information from the current drive.");
        }
        catch (DriveNotFoundException) {
            return DriveReadResult.Failure("Current drive was not found.");
        }
        catch (IOException ex) {
            return DriveReadResult.Failure($"I/O error while reading drive: {ex.Message}");
        }
        catch (Exception ex) {
            return DriveReadResult.Failure($"Unexpected error while reading drive: {ex.Message}");
        }
    }
}

public sealed record DriveReadResult(bool IsSuccess, DriveSnapshot? Snapshot, string? ErrorMessage)
{
    public static DriveReadResult Success(DriveSnapshot snapshot) => new(true, snapshot, null);

    public static DriveReadResult Failure(string message) => new(false, null, message);
}
