namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class TestStepTemplateRecord
{
    public Guid Id { get; set; }

    public Guid TestCaseTemplateId { get; set; }

    public string StepText { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public TestCaseTemplateRecord? TestCaseTemplate { get; set; }
}
