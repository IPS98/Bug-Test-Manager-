using System.Collections.ObjectModel;

namespace BugTestManager.App.ViewModels;

public sealed class TestSuiteRevisionItemViewModel
{
    public TestSuiteRevisionItemViewModel(
        Guid id,
        string name,
        DateOnly? effectiveDate,
        IEnumerable<TemplateSectionItemViewModel> sections)
    {
        Id = id;
        Name = name;
        EffectiveDate = effectiveDate;
        Sections = new ObservableCollection<TemplateSectionItemViewModel>(sections);
    }

    public Guid Id { get; }

    public string Name { get; }

    public DateOnly? EffectiveDate { get; }

    public string EffectiveDateDisplay => EffectiveDate?.ToString("yyyy-MM-dd") ?? string.Empty;

    public ObservableCollection<TemplateSectionItemViewModel> Sections { get; }
}
