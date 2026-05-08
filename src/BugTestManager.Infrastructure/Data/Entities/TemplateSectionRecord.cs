namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TemplateSectionRecord
{
    public Guid Id { get; set; }

    public Guid TestSuiteId { get; set; }

    public Guid? TestSuiteRevisionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TestSuiteRecord? TestSuite { get; set; }

    public TestSuiteRevisionRecord? TestSuiteRevision { get; set; }

    public List<TestCaseTemplateRecord> TestCases { get; set; } = [];
}
