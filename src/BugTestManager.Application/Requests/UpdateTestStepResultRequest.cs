using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record UpdateTestStepResultRequest(
    Guid TestStepResultId,
    TestResultStatus Status,
    string Comment);
