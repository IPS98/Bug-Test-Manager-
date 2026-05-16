using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestCaseResultRecord
{
    public Guid Id { get; set; }

    public Guid TestSectionResultId { get; set; }

    public Guid TestCaseTemplateId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TestResultStatus Status { get; set; }

    public DateTimeOffset? LastStatusChangedAt { get; set; }

    public string Comment { get; set; } = string.Empty;

    public TestSectionResultRecord? TestSectionResult { get; set; }

    public List<TestStepResultRecord> Steps { get; set; } = [];
}
