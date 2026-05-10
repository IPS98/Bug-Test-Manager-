using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record UpdateTestCaseResultRequest(
    Guid TestCaseResultId,
    TestResultStatus Status,
    string Comment);
