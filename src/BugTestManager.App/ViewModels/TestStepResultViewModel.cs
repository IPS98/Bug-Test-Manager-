using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestStepResultViewModel : ObservableObject
{
    public TestStepResultViewModel(
        Guid id,
        Guid testStepTemplateId,
        string stepText,
        string expectedResult,
        int sortOrder,
        TestResultStatus status,
        DateTimeOffset? lastStatusChangedAt,
        string comment,
        IEnumerable<LinkedBugSummaryViewModel> linkedBugs)
    {
        Id = id;
        TestStepTemplateId = testStepTemplateId;
        StepText = stepText;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Status = status;
        LastStatusChangedAt = lastStatusChangedAt;
        Comment = comment;
        LinkedBugs = new ObservableCollection<LinkedBugSummaryViewModel>(linkedBugs);
    }

    public Guid Id { get; }

    public Guid TestStepTemplateId { get; }

    public string StepText { get; }

    public string CheckText => StepText;

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public TestResultStatus Status { get; }

    public DateTimeOffset? LastStatusChangedAt { get; }

    public string Comment { get; }

    public ObservableCollection<LinkedBugSummaryViewModel> LinkedBugs { get; }

    public string StatusDisplay => TestResultStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => TestResultStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => TestResultStatusDisplayNames.ForegroundForStatus(Status);

    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "No comment yet" : Comment;

    public string LastStatusChangedAtDisplay => LastStatusChangedAt is null
        ? "Not changed yet"
        : LastStatusChangedAt.Value
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public Visibility LastStatusChangedAtVisibility => LastStatusChangedAt is null
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility LinkedBugBadgeVisibility => LinkedBugs.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string LinkedBugCountDisplay => LinkedBugs.Count == 1
        ? "1 linked bug"
        : $"{LinkedBugs.Count} linked bugs";

    public string LinkedBugSummaryDisplay => LinkedBugs.Count == 0
        ? "No linked bugs"
        : string.Join(", ", LinkedBugs.Take(2).Select(bug => bug.Title))
            + (LinkedBugs.Count > 2 ? $" +{LinkedBugs.Count - 2} more" : string.Empty);

    private int unreadDiscussionCount;

    public int UnreadDiscussionCount
    {
        get => unreadDiscussionCount;
        set
        {
            if (SetProperty(ref unreadDiscussionCount, value))
            {
                OnPropertyChanged(nameof(DiscussionBadgeVisibility));
                OnPropertyChanged(nameof(DiscussionBadgeText));
            }
        }
    }

    public Visibility DiscussionBadgeVisibility => UnreadDiscussionCount > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string DiscussionBadgeText => UnreadDiscussionCount > 99
        ? "99+"
        : UnreadDiscussionCount.ToString();
}
