using System.Globalization;
using DskSpc.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DskSpc.UI;

public sealed class DashboardRenderer
{
    public IRenderable Render(
        DriveSnapshot? snapshot,
        string? errorMessage,
        IReadOnlyList<DriveSnapshot> history,
        int historyCapacity,
        int intervalSeconds,
        int historyWindowStart,
        int historyWindowSize)
    {
        var header = BuildHeader(intervalSeconds);
        var freePanel = BuildFreePanel(snapshot);
        var currentPanel = BuildCurrentPanel(snapshot, errorMessage);
        var historyPanel = BuildHistoryPanel(history, historyCapacity, historyWindowStart, historyWindowSize);

        var leftColumn = new Rows(
            freePanel,
            currentPanel);

        var body = new Grid();
        body.AddColumn();
        body.AddColumn();
        body.AddRow(leftColumn, historyPanel);

        return new Rows(header, Align.Center(body));
    }

    private static Panel BuildHeader(int intervalSeconds)
    {
        var content = Align.Center(new Markup(
            $"[bold cyan]Disk Space Dashboard[/]  [grey]Updates every {intervalSeconds}s on screen, history saved on free-space changes | Ctrl+C to exit[/]"));

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Expand();
    }

    private static Panel BuildFreePanel(DriveSnapshot? snapshot)
    {
        var freeColor = GetFreeColor(snapshot);
        string figletText = snapshot is null ? "NA" : $"{FormatFigletGb(snapshot.FreeBytes)}GB";

        var content = new Rows(
            new Markup(" "),
            new FigletText(figletText)
                .Color(freeColor)
                .Centered(),
            BuildUsageBar(snapshot));

        return new Panel(content)
            .Header(" Free Space ", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(freeColor);
    }

    private static IRenderable BuildUsageBar(DriveSnapshot? snapshot)
    {
        var chart = new BarChart()
            .Width(60)
            .Label("[grey]Uso do disco (%)[/]")
            .CenterLabel();

        if (snapshot is null) {
            chart.AddItem("No data", 100, Color.Grey37);
            return chart;
        }

        chart
            .AddItem("Free %", Math.Round(snapshot.FreePercent, 2), Color.SpringGreen2)
            .AddItem("Used %", Math.Round(snapshot.UsedPercent, 2), Color.Orange3);

        return chart;
    }

    private static Panel BuildCurrentPanel(DriveSnapshot? snapshot, string? errorMessage)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value")
            .Expand();

        if (!string.IsNullOrWhiteSpace(errorMessage)) {
            table.AddRow("Status", "[red]Read failed[/]");
            table.AddRow("Error", $"[red]{Markup.Escape(errorMessage)}[/]");
        }

        if (snapshot is null) {
            table.AddRow("Drive", "--");
            table.AddRow("Used", "--");
            table.AddRow("Free", "--");
            table.AddRow("Total", "--");
            table.AddRow("Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else {
            table.AddRow("Drive", snapshot.Drive);
            table.AddRow("Used", FormatGb(snapshot.UsedBytes));
            table.AddRow("Free", FormatGb(snapshot.FreeBytes));
            table.AddRow("Total", FormatGb(snapshot.TotalBytes));
            table.AddRow("Used %", $"{snapshot.UsedPercent:N2}%");
            table.AddRow("Free %", $"{snapshot.FreePercent:N2}%");
            table.AddRow("Timestamp", snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        return new Panel(table)
            .Header(" Current Reading ", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Expand();
    }

    private static Panel BuildHistoryPanel(
        IReadOnlyList<DriveSnapshot> history,
        int historyCapacity,
        int historyWindowStart,
        int historyWindowSize)
    {
        int visibleRows = Math.Clamp(historyWindowSize, 1, historyCapacity);
        int maxWindowStart = Math.Max(1, historyCapacity - visibleRows + 1);
        int safeWindowStart = Math.Clamp(historyWindowStart, 1, maxWindowStart);
        int windowEnd = safeWindowStart + visibleRows - 1;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("#")
            .AddColumn("Time")
            .AddColumn("Free");

        for (int offset = 0; offset < visibleRows; offset++) {
            int slotIndex = safeWindowStart + offset;
            string indexCell = FormatHistoryCell(slotIndex, slotIndex.ToString());

            if (slotIndex <= history.Count) {
                var item = history[slotIndex - 1];

                table.AddRow(
                    indexCell,
                    FormatHistoryCell(slotIndex, item.Timestamp.ToString("HH:mm:ss")),
                    FormatHistoryCell(slotIndex, $"{FreeSpacePrecision.FormatGb(item.FreeBytes)} GB"));
            }
            else {
                table.AddRow(
                    indexCell,
                    FormatHistoryCell(slotIndex, string.Empty),
                    FormatHistoryCell(slotIndex, string.Empty));
            }
        }

        return new Panel(table)
            .Header($"History ({safeWindowStart}-{windowEnd} of {historyCapacity}) ↑ ↓", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);
    }

    private static Color GetFreeColor(DriveSnapshot? snapshot)
    {
        if (snapshot is null) {
            return Color.Grey;
        }

        if (snapshot.FreePercent < 10) {
            return Color.Red;
        }

        if (snapshot.FreePercent < 20) {
            return Color.Yellow;
        }

        return Color.SpringGreen2;
    }

    private static string FormatGb(long bytes)
    {
        return $"{ToGb(bytes):N2} GB";
    }

    private static string FormatFigletGb(long bytes)
    {
        return FreeSpacePrecision.FormatGb(bytes);
    }

    private static string FormatHistoryCell(int slotIndex, string value)
    {
        string safeValue = Markup.Escape(value);

        if (slotIndex != 1) {
            return safeValue;
        }

        return $"[springgreen2]{(safeValue.Length == 0 ? " " : safeValue)}[/]";
    }

    private static double ToGb(long bytes)
    {
        return bytes / 1024d / 1024d / 1024d;
    }
}
