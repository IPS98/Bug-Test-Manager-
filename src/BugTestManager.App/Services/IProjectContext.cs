using BugTestManager.Application.Defaults;

namespace BugTestManager.App.Services;

public interface IProjectContext
{
    Guid CurrentProjectId { get; set; }
}
