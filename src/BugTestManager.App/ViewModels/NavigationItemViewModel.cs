using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(AppPageKey pageKey, string name, string description, string iconGlyph)
    {
        PageKey = pageKey;
        Name = name;
        Description = description;
        IconGlyph = iconGlyph;
    }

    public AppPageKey PageKey { get; }

    public string Name { get; }

    public string Description { get; }

    public string IconGlyph { get; }

    [ObservableProperty]
    private bool isSelected;
}
