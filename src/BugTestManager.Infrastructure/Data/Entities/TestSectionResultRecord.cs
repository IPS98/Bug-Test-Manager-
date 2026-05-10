namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestSectionResultRecord
{
    public Guid Id { get; set; }

    public Guid TestSessionId { get; set; }

    public Guid TemplateSectionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TestSessionRecord? TestSession { get; set; }

    public List<TestCaseResultRecord> TestCases { get; set; } = [];
}
