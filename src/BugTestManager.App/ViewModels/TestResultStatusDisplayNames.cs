using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public static class TestResultStatusDisplayNames
{
    private static readonly Brush NotTestedBackground = CreateBrush("#F1F5F9");
    private static readonly Brush NotTestedForeground = CreateBrush("#475569");
    private static readonly Brush PassBackground = CreateBrush("#DCFCE7");
    private static readonly Brush PassForeground = CreateBrush("#166534");
    private static readonly Brush FailBackground = CreateBrush("#FEE2E2");
    private static readonly Brush FailForeground = CreateBrush("#991B1B");
    private static readonly Brush BlockedBackground = CreateBrush("#FEF3C7");
    private static readonly Brush BlockedForeground = CreateBrush("#92400E");
    private static readonly Brush NotApplicableBackground = CreateBrush("#E0E7FF");
    private static readonly Brush NotApplicableForeground = CreateBrush("#3730A3");

    public static string ForStatus(TestResultStatus status)
    {
        return status switch
        {
            TestResultStatus.NotTested => "Not Tested",
            TestResultStatus.Pass => "Pass",
            TestResultStatus.Fail => "Fail",
            TestResultStatus.Blocked => "Blocked",
            TestResultStatus.NotApplicable => "N/A",
            _ => status.ToString()
        };
    }

    public static Brush BackgroundForStatus(TestResultStatus status)
    {
        return status switch
        {
            TestResultStatus.Pass => PassBackground,
            TestResultStatus.Fail => FailBackground,
            TestResultStatus.Blocked => BlockedBackground,
            TestResultStatus.NotApplicable => NotApplicableBackground,
            _ => NotTestedBackground
        };
    }

    public static Brush ForegroundForStatus(TestResultStatus status)
    {
        return status switch
        {
            TestResultStatus.Pass => PassForeground,
            TestResultStatus.Fail => FailForeground,
            TestResultStatus.Blocked => BlockedForeground,
            TestResultStatus.NotApplicable => NotApplicableForeground,
            _ => NotTestedForeground
        };
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
