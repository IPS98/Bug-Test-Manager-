using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed class TestStepResultViewModel
{
    public TestStepResultViewModel(
        Guid id,
        string stepText,
        string expectedResult,
        int sortOrder,
        TestResultStatus status,
        string comment)
    {
        Id = id;
        StepText = stepText;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Status = status;
        Comment = comment;
    }

    public Guid Id { get; }

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
}
