using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestCaseTemplateItemViewModel
{
    public TestCaseTemplateItemViewModel(
        Guid id,
        string title,
        string expectedResult,
        int sortOrder,
        IEnumerable<TestStepTemplateItemViewModel> steps)
    {
        Id = id;
        Title = title;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Steps = new ObservableCollection<TestStepTemplateItemViewModel>(steps);
    }

    public Guid Id { get; }

    public string Title { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public ObservableCollection<TestStepTemplateItemViewModel> Steps { get; }
}
