using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public static class BugStatusDisplayNames
{
    private static readonly Brush OpenBackground = CreateBrush("#FEE2E2");
    private static readonly Brush OpenForeground = CreateBrush("#991B1B");
    private static readonly Brush ActiveBackground = CreateBrush("#FEF3C7");
    private static readonly Brush ActiveForeground = CreateBrush("#92400E");
    private static readonly Brush FixedBackground = CreateBrush("#DBEAFE");
    private static readonly Brush FixedForeground = CreateBrush("#1D4ED8");
    private static readonly Brush ClosedBackground = CreateBrush("#DCFCE7");
    private static readonly Brush ClosedForeground = CreateBrush("#166534");
    private static readonly Brush NeutralBackground = CreateBrush("#F1F5F9");
    private static readonly Brush NeutralForeground = CreateBrush("#475569");

    public static string ForStatus(BugStatus status)
    {
        return status switch
        {
            BugStatus.Open => "Open",
            BugStatus.InProgress => "In Progress",
            BugStatus.Fixed => "Fixed",
            BugStatus.ReadyForRetest => "Ready for Retest",
            BugStatus.Reopened => "Reopened",
            BugStatus.Closed => "Closed",
            BugStatus.Rejected => "Rejected",
            _ => status.ToString()
        };
    }

    public static Brush BackgroundForStatus(BugStatus status)
    {
        return status switch
        {
            BugStatus.Open or BugStatus.Reopened => OpenBackground,
            BugStatus.InProgress or BugStatus.ReadyForRetest => ActiveBackground,
            BugStatus.Fixed => FixedBackground,
            BugStatus.Closed => ClosedBackground,
            _ => NeutralBackground
        };
    }

    public static Brush ForegroundForStatus(BugStatus status)
    {
        return status switch
        {
            BugStatus.Open or BugStatus.Reopened => OpenForeground,
            BugStatus.InProgress or BugStatus.ReadyForRetest => ActiveForeground,
            BugStatus.Fixed => FixedForeground,
            BugStatus.Closed => ClosedForeground,
            _ => NeutralForeground
        };
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
