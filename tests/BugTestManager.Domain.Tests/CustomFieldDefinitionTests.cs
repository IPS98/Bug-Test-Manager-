using BugTestManager.Domain.Entities;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Tests;

public sealed class CustomFieldDefinitionTests
{
    [Fact]
    public void Constructor_CanCreateRequiredFieldForSpecificEntityType()
    {
        var field = new CustomFieldDefinition(
            EntityReferenceType.TestCaseTemplate,
            "Firmware Version",
            FieldType.Text,
            isRequired: true,
            sortOrder: 1);

        Assert.Equal(EntityReferenceType.TestCaseTemplate, field.TargetEntityType);
        Assert.True(field.IsRequired);
        Assert.Equal("Firmware Version", field.Name);
    }

    [Fact]
    public void AddOption_DoesNotAllowDuplicateOptions()
    {
        var field = new CustomFieldDefinition(
            EntityReferenceType.BugReport,
            "Severity",
            FieldType.SingleSelect,
            options: ["High"]);

        Assert.Throws<InvalidOperationException>(() => field.AddOption("high"));
    }

    [Fact]
    public void Constructor_AllowsOptionalFieldForAnotherTeamWorkflow()
    {
        var field = new CustomFieldDefinition(
            EntityReferenceType.TestSession,
            "Oscilloscope Screenshot",
            FieldType.AttachmentReference,
            isRequired: false);

        Assert.False(field.IsRequired);
    }
}
