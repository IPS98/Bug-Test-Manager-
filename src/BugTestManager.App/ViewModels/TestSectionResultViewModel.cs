using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestSectionResultViewModel
{
    public TestSectionResultViewModel(
        Guid id,
        string name,
        string category,
        int sortOrder,
        IEnumerable<TestCaseResultViewModel> testCases)
    {
        Id = id;
        Name = name;
        Category = category;
        SortOrder = sortOrder;
        TestCases = new ObservableCollection<TestCaseResultViewModel>(testCases);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Category { get; }

    public int SortOrder { get; }

    public ObservableCollection<TestCaseResultViewModel> TestCases { get; }

    public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "No category" : Category;

    public string SizeDisplay => $"{TestCases.Count} cases";
}
