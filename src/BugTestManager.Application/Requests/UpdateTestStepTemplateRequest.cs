namespace BugTestManager.Application.Requests;

public sealed record UpdateTestStepTemplateRequest(
    Guid TestStepId,
    string StepText,
    string ExpectedResult);
