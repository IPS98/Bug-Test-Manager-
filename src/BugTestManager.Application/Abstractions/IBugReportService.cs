using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;

namespace BugTestManager.Application.Abstractions;

public interface IBugReportService
{
    IReadOnlyList<BugReportItem> GetBugs(Guid? projectId = null);

    Guid CreateBug(CreateBugReportRequest request);

    void UpdateBug(UpdateBugReportRequest request);

    void UpdateStatus(UpdateBugStatusRequest request);
}
