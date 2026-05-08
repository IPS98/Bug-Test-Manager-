using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TemplateSectionItemViewModel
{
    public TemplateSectionItemViewModel(
        Guid id,
        string name,
        string category,
        int sortOrder,
        IEnumerable<TestCaseTemplateItemViewModel> testCases)
    {
        Id = id;
        Name = name;
        Category = category;
        SortOrder = sortOrder;
        TestCases = new ObservableCollection<TestCaseTemplateItemViewModel>(testCases);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Category { get; }

    public int SortOrder { get; }

    public ObservableCollection<TestCaseTemplateItemViewModel> TestCases { get; }
}
