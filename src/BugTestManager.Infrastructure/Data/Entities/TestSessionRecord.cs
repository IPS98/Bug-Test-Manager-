namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestSessionRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid TestSuiteId { get; set; }

    public Guid? TestSuiteRevisionId { get; set; }

    public bool IsManual { get; set; }

    public string TestSuiteName { get; set; } = string.Empty;

    public string? TestSuiteRevisionName { get; set; }

    public string TestedVersion { get; set; } = string.Empty;

    public string BuildNumber { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public List<TestSectionResultRecord> Sections { get; set; } = [];
}
