using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestSuiteItemViewModel
{
    public TestSuiteItemViewModel(
        string name,
        string description,
        IEnumerable<TestSuiteRevisionItemViewModel> revisions)
    {
        Name = name;
        Description = description;
        Revisions = new ObservableCollection<TestSuiteRevisionItemViewModel>(revisions);
    }

    public string Name { get; }

    public string Description { get; }

    public ObservableCollection<TestSuiteRevisionItemViewModel> Revisions { get; }
}
