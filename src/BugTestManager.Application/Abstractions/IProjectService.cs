using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;

namespace BugTestManager.Application.Abstractions;

public interface IProjectService
{
    IReadOnlyList<ProjectItem> GetProjects();

    Guid CreateProject(CreateProjectRequest request);

    void DeleteProject(Guid projectId);
}
