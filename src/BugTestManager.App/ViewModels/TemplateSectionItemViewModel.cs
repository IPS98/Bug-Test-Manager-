using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TemplateSectionItemViewModel
{
    public TemplateSectionItemViewModel(
        string name,
        string category,
        int sortOrder,
        IEnumerable<TestCaseTemplateItemViewModel> testCases)
    {
        Name = name;
        Category = category;
        SortOrder = sortOrder;
        TestCases = new ObservableCollection<TestCaseTemplateItemViewModel>(testCases);
    }

    public string Name { get; }

    public string Category { get; }

    public int SortOrder { get; }

    public ObservableCollection<TestCaseTemplateItemViewModel> TestCases { get; }
}
