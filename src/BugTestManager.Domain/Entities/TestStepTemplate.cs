using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TestStepTemplate
{
    public TestStepTemplate(Guid testCaseTemplateId, string stepText, string? expectedResult = null, int sortOrder = 0)
    {
        Id = Guid.NewGuid();
        TestCaseTemplateId = Guard.Required(testCaseTemplateId, nameof(testCaseTemplateId), "Test case template id");
        StepText = Guard.Required(stepText, nameof(stepText), "Step text");
        ExpectedResult = expectedResult?.Trim() ?? string.Empty;
        SortOrder = Guard.NotNegative(sortOrder, nameof(sortOrder), "Sort order");
    }

    public Guid Id { get; }

    public Guid TestCaseTemplateId { get; }

    public string StepText { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }
}
