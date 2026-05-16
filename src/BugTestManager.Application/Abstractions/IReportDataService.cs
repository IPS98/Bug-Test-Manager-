using BugTestManager.Application.ReadModels;

namespace BugTestManager.Application.Abstractions;

public interface IReportDataService
{
    TestSessionReportItem GetTestSessionReport(Guid testSessionId, Guid? projectId = null);
}
