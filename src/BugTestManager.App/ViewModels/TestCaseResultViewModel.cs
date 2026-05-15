using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestCaseResultViewModel : ObservableObject
{
    public TestCaseResultViewModel(
        Guid id,
        Guid testCaseTemplateId,
        string title,
        string expectedResult,
        int sortOrder,
        TestResultStatus status,
        string comment,
        IEnumerable<TestStepResultViewModel> steps)
    {
        Id = id;
        TestCaseTemplateId = testCaseTemplateId;
        Title = title;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Status = status;
        Comment = comment;
        Steps = new ObservableCollection<TestStepResultViewModel>(steps);
    }

    public Guid Id { get; }

    public Guid TestCaseTemplateId { get; }

    public string Title { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public TestResultStatus Status { get; }

    public string Comment { get; }

    public ObservableCollection<TestStepResultViewModel> Steps { get; }

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

    public string StatusDisplay => TestResultStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => TestResultStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => TestResultStatusDisplayNames.ForegroundForStatus(Status);

    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "No comment yet" : Comment;

    public string CheckCountDisplay => $"{Steps.Count} checks";

    public Visibility DiscussionBadgeVisibility => UnreadDiscussionCount > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string DiscussionBadgeText => UnreadDiscussionCount > 99
        ? "99+"
        : UnreadDiscussionCount.ToString();
}
