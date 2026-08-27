using DskSpc.Models;
using DskSpc.Services;
using DskSpc.UI;
using Spectre.Console;

const int IntervalSeconds = 20;
const int HistoryCapacity = 50;
const int HistoryWindowSize = 19;

var driveService = new DriveService();
var historyManager = new HistoryManager(HistoryCapacity);
var renderer = new DashboardRenderer();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

DriveSnapshot? latestSnapshot = null;
string? latestError = null;
int historyWindowStart = 1;
int maxHistoryWindowStart = Math.Max(1, HistoryCapacity - HistoryWindowSize + 1);
long? lastReadFreeSpaceUnits = null;

AnsiConsole.Clear();

await AnsiConsole.Live(renderer.Render(
        snapshot: latestSnapshot,
        errorMessage: latestError,
        history: historyManager.GetNewestFirst(),
        historyCapacity: HistoryCapacity,
        intervalSeconds: IntervalSeconds,
        historyWindowStart: historyWindowStart,
        historyWindowSize: HistoryWindowSize))
    .AutoClear(false)
    .StartAsync(async ctx => {
        DateTime nextReadAt = DateTime.UtcNow;

        while (!cts.Token.IsCancellationRequested) {
            bool shouldRefresh = false;

            if (DateTime.UtcNow >= nextReadAt) {
                DriveReadResult result = driveService.TryReadCurrentDrive();

                if (result.IsSuccess && result.Snapshot is not null) {
                    latestSnapshot = result.Snapshot;
                    latestError = null;
                    long currentFreeSpaceUnits = FreeSpacePrecision.ToComparableUnits(result.Snapshot.FreeBytes);

                    bool isFirstSuccessfulRead = lastReadFreeSpaceUnits is null;
                    bool freeSpaceChanged = !isFirstSuccessfulRead && lastReadFreeSpaceUnits != currentFreeSpaceUnits;

                    if (isFirstSuccessfulRead || freeSpaceChanged) {
                        historyManager.Add(result.Snapshot);
                    }

                    lastReadFreeSpaceUnits = currentFreeSpaceUnits;
                }
                else {
                    latestError = result.ErrorMessage ?? "Unknown failure while reading drive.";
                }

                nextReadAt = DateTime.UtcNow.AddSeconds(IntervalSeconds);
                shouldRefresh = true;
            }

            while (TryReadKey(out ConsoleKey key)) {
                if (key == ConsoleKey.UpArrow) {
                    int nextWindowStart = Math.Max(1, historyWindowStart - 1);
                    if (nextWindowStart != historyWindowStart) {
                        historyWindowStart = nextWindowStart;
                        shouldRefresh = true;
                    }
                }
                else if (key == ConsoleKey.DownArrow) {
                    int nextWindowStart = Math.Min(maxHistoryWindowStart, historyWindowStart + 1);
                    if (nextWindowStart != historyWindowStart) {
                        historyWindowStart = nextWindowStart;
                        shouldRefresh = true;
                    }
                }
            }

            if (shouldRefresh) {
                ctx.UpdateTarget(renderer.Render(
                    snapshot: latestSnapshot,
                    errorMessage: latestError,
                    history: historyManager.GetNewestFirst(),
                    historyCapacity: HistoryCapacity,
                    intervalSeconds: IntervalSeconds,
                    historyWindowStart: historyWindowStart,
                    historyWindowSize: HistoryWindowSize));
                ctx.Refresh();
            }

            try {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
            catch (OperationCanceledException) {
                break;
            }
        }
    });

AnsiConsole.MarkupLine("[grey]Application finished.[/]");

static bool TryReadKey(out ConsoleKey key)
{
    key = default;

    try {
        if (!Console.KeyAvailable) {
            return false;
        }

        key = Console.ReadKey(intercept: true).Key;
        return true;
    }
    catch (InvalidOperationException) {
        return false;
    }
}

