namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestSuiteRevisionRecord
{
    public Guid Id { get; set; }

    public Guid TestSuiteId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly? EffectiveDate { get; set; }

    public int SortOrder { get; set; }

    public TestSuiteRecord? TestSuite { get; set; }

    public List<TemplateSectionRecord> Sections { get; set; } = [];
}
