using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record TestSessionSummaryItem(
    Guid Id,
    string Name,
    string TestSuiteName,
    string? TestSuiteRevisionName,
    string TestedVersion,
    string BuildNumber,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    int SectionCount,
    int TestCaseCount,
    int StepCount);

public sealed record TestSessionDetailsItem(
    Guid Id,
    string Name,
    string TestSuiteName,
    string? TestSuiteRevisionName,
    string TestedVersion,
    string BuildNumber,
    string Notes,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TestSectionResultItem> Sections);

public sealed record TestSectionResultItem(
    Guid Id,
    string Name,
    string Category,
    int SortOrder,
    IReadOnlyList<TestCaseResultItem> TestCases);

public sealed record TestCaseResultItem(
    Guid Id,
    string Title,
    string ExpectedResult,
    int SortOrder,
    TestResultStatus Status,
    string Comment,
    IReadOnlyList<TestStepResultItem> Steps);

public sealed record TestStepResultItem(
    Guid Id,
    string StepText,
    string ExpectedResult,
    int SortOrder,
    TestResultStatus Status,
    string Comment);
