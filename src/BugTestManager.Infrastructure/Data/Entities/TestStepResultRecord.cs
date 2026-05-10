using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestStepResultRecord
{
    public Guid Id { get; set; }

    public Guid TestCaseResultId { get; set; }

    public Guid TestStepTemplateId { get; set; }

    public string StepText { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TestResultStatus Status { get; set; }

    public string Comment { get; set; } = string.Empty;

    public TestCaseResultRecord? TestCaseResult { get; set; }
}
