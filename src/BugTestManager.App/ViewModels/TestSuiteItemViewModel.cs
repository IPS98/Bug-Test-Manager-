using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestSuiteItemViewModel
{
    public TestSuiteItemViewModel(
        string name,
        string description,
        bool revisionIsRequired,
        IEnumerable<TestSuiteRevisionItemViewModel> revisions)
    {
        Name = name;
        Description = description;
        RevisionIsRequired = revisionIsRequired;
        Revisions = new ObservableCollection<TestSuiteRevisionItemViewModel>(revisions);
    }

    public string Name { get; }

    public string Description { get; }

    public bool RevisionIsRequired { get; }

    public string RevisionPolicyDisplay => RevisionIsRequired ? "Revision required" : "Revision optional";

    public ObservableCollection<TestSuiteRevisionItemViewModel> Revisions { get; }
}
