using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;

namespace BugTestManager.Application.Abstractions;

public interface ITestSessionTemplateSyncService
{
    TestSessionTemplateSyncPreview GetPreview(Guid testSessionId, Guid? projectId = null);

    TestSessionTemplateSyncResult UpdateOriginalTemplate(UpdateTemplateFromSessionRequest request);

    TestSessionTemplateSyncResult CreateTemplateFromSession(CreateTemplateFromSessionRequest request);
}
