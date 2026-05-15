namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestSuiteRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool RevisionIsRequired { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<TestSuiteRevisionRecord> Revisions { get; set; } = [];

    public List<TemplateSectionRecord> Sections { get; set; } = [];
}
