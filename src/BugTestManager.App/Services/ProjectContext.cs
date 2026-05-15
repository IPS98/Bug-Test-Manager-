using BugTestManager.Application.Defaults;

namespace BugTestManager.App.Services;

public sealed class ProjectContext : IProjectContext
{
    public Guid CurrentProjectId { get; set; } = ProjectDefaults.DefaultProjectId;
}
