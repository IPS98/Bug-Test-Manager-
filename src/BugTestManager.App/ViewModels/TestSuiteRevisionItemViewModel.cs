using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestSuiteRevisionItemViewModel
{
    public TestSuiteRevisionItemViewModel(
        string name,
        DateOnly? effectiveDate,
        IEnumerable<TemplateSectionItemViewModel> sections)
    {
        Name = name;
        EffectiveDate = effectiveDate;
        Sections = new ObservableCollection<TemplateSectionItemViewModel>(sections);
    }

    public string Name { get; }

    public DateOnly? EffectiveDate { get; }

    public string EffectiveDateDisplay => EffectiveDate?.ToString("yyyy-MM-dd") ?? "No date";

    public ObservableCollection<TemplateSectionItemViewModel> Sections { get; }
}
