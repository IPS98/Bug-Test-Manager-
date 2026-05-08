using System.Collections.ObjectModel;
using BugTestManager.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(IUserContext userContext)
    {
        CurrentUserDisplay = $"Signed in as {userContext.UserName} ({userContext.Role})";

        Modules =
        [
            new NavigationItemViewModel("Templates", "Reusable test suites, revisions, sections, cases, and steps."),
            new NavigationItemViewModel("Test Sessions", "Manual test reports, statuses, photos, comments, and dates."),
            new NavigationItemViewModel("Bugs", "Bug reports, developer comments, status changes, and retesting."),
            new NavigationItemViewModel("Reports", "Readable PDF reports for versions, revisions, tests, and bugs.")
        ];
    }

    public string Title => "Bug & Test Manager";

    public string CurrentUserDisplay { get; }

    public ObservableCollection<NavigationItemViewModel> Modules { get; }
}
