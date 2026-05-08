namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestCaseTemplateRecord
{
    public Guid Id { get; set; }

    public Guid TemplateSectionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TemplateSectionRecord? TemplateSection { get; set; }

    public List<TestStepTemplateRecord> Steps { get; set; } = [];
}
