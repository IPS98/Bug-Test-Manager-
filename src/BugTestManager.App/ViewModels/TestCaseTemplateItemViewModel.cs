using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestCaseTemplateItemViewModel
{
    public TestCaseTemplateItemViewModel(
        string title,
        string expectedResult,
        int sortOrder,
        IEnumerable<TestStepTemplateItemViewModel> steps)
    {
        Title = title;
        ExpectedResult = expectedResult;
        SortOrder = sortOrder;
        Steps = new ObservableCollection<TestStepTemplateItemViewModel>(steps);
    }

    public string Title { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public ObservableCollection<TestStepTemplateItemViewModel> Steps { get; }
}
