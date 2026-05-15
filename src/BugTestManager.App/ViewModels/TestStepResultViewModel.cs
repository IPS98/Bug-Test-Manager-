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
        string comment)
    {
        Id = id;
        TestStepTemplateId = testStepTemplateId;
        StepText = stepText;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Status = status;
        Comment = comment;
    }

    public Guid Id { get; }

    public Guid TestStepTemplateId { get; }

    public string StepText { get; }

    public string CheckText => StepText;

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public TestResultStatus Status { get; }

    public string Comment { get; }

    public string StatusDisplay => TestResultStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => TestResultStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => TestResultStatusDisplayNames.ForegroundForStatus(Status);

    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "No comment yet" : Comment;

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
