namespace BugTestManager.Application.Results;

public sealed record TestSessionTemplateSyncResult(
    Guid TestSuiteId,
    string TestSuiteName,
    int AddedSections,
    int AddedTestCases,
    int AddedChecks);
