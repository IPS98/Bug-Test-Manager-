using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(AppPageKey pageKey, string name, string description)
    {
        PageKey = pageKey;
        Name = name;
        Description = description;
    }

    public AppPageKey PageKey { get; }

    public string Name { get; }

    public string Description { get; }

    [ObservableProperty]
    private bool isSelected;
}
