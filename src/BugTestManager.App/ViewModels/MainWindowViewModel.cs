using System.Collections.ObjectModel;
using BugTestManager.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly TestSuitesViewModel testSuitesViewModel;

    public MainWindowViewModel(IUserContext userContext, TestSuitesViewModel testSuitesViewModel)
    {
        this.testSuitesViewModel = testSuitesViewModel;
        CurrentUserDisplay = $"Signed in as {userContext.UserName} ({userContext.Role})";

        Modules =
        [
            new NavigationItemViewModel(AppPageKey.Templates, "Templates", "Reusable test suites, revisions, sections, cases, and steps."),
            new NavigationItemViewModel(AppPageKey.TestSessions, "Test Sessions", "Manual test reports, statuses, photos, comments, and dates."),
            new NavigationItemViewModel(AppPageKey.Bugs, "Bugs", "Bug reports, developer comments, status changes, and retesting."),
            new NavigationItemViewModel(AppPageKey.Reports, "Reports", "Readable PDF reports for versions, revisions, tests, and bugs.")
        ];

        SelectModule(Modules[0]);
    }

    public string Title => "Bug & Test Manager";

    public string CurrentUserDisplay { get; }

    public ObservableCollection<NavigationItemViewModel> Modules { get; }

    [ObservableProperty]
    private string currentPageTitle = string.Empty;

    [ObservableProperty]
    private string currentPageDescription = string.Empty;

    [ObservableProperty]
    private object? currentPage;

    [RelayCommand]
    private void SelectModule(NavigationItemViewModel module)
    {
        foreach (var navigationItem in Modules)
        {
            navigationItem.IsSelected = navigationItem == module;
        }

        CurrentPageTitle = module.Name;
        CurrentPageDescription = module.Description;
        CurrentPage = module.PageKey == AppPageKey.Templates
            ? testSuitesViewModel
            : new PlaceholderPageViewModel(module.Name, "This page will be implemented after the template browser is stable.");
    }
}
