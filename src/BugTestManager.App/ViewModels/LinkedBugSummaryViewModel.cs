using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed class LinkedBugSummaryViewModel
{
    public LinkedBugSummaryViewModel(Guid id, string title, BugStatus status)
    {
        Id = id;
        Title = title;
        Status = status;
    }

    public Guid Id { get; }

    public string Title { get; }

    public BugStatus Status { get; }

    public string StatusDisplay => BugStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => BugStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => BugStatusDisplayNames.ForegroundForStatus(Status);
}
