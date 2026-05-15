namespace BugTestManager.App.ViewModels;

public sealed class ProjectOptionViewModel
{
    public ProjectOptionViewModel(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }

    public string DisplayName => Name;
}
