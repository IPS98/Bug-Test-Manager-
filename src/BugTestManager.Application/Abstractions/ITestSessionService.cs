using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;

namespace BugTestManager.Application.Abstractions;

public interface ITestSessionService
{
    IReadOnlyList<TestSessionSummaryItem> GetSessions();

    TestSessionDetailsItem GetSession(Guid testSessionId);

    Guid CreateSession(CreateTestSessionRequest request);

    void UpdateTestCaseResult(UpdateTestCaseResultRequest request);

    void UpdateTestStepResult(UpdateTestStepResultRequest request);
}
