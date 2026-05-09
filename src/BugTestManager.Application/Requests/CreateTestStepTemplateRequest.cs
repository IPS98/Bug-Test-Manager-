namespace BugTestManager.Application.Requests;

public sealed record CreateTestStepTemplateRequest(
    Guid TestCaseTemplateId,
    string StepText,
    string ExpectedResult);
