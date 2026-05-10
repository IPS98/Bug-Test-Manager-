using System.Collections.ObjectModel;
using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed class TestCaseResultViewModel
{
    public TestCaseResultViewModel(
        Guid id,
        string title,
        string expectedResult,
        int sortOrder,
        TestResultStatus status,
        string comment,
        IEnumerable<TestStepResultViewModel> steps)
    {
        Id = id;
        Title = title;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Status = status;
        Comment = comment;
        Steps = new ObservableCollection<TestStepResultViewModel>(steps);
    }

    public Guid Id { get; }

    public string Title { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public TestResultStatus Status { get; }

    public string Comment { get; }

    public ObservableCollection<TestStepResultViewModel> Steps { get; }

    public string StatusDisplay => TestResultStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => TestResultStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => TestResultStatusDisplayNames.ForegroundForStatus(Status);

    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "No comment yet" : Comment;

    public string CheckCountDisplay => $"{Steps.Count} checks";
}
