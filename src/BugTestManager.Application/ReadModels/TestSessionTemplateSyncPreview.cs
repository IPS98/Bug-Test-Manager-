namespace BugTestManager.Application.ReadModels;

public sealed record TestSessionTemplateSyncPreview(
    Guid TestSessionId,
    string SessionName,
    Guid? OriginalTestSuiteId,
    string OriginalTemplateName,
    string? OriginalRevisionName,
    bool CanUpdateOriginalTemplate,
    int TotalSections,
    int TotalTestCases,
    int TotalChecks,
    int NewSectionCount,
    int NewTestCaseCount,
    int NewCheckCount,
    IReadOnlyList<string> Notes);
