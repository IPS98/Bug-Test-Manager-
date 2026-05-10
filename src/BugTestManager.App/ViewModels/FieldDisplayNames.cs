using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public static class FieldDisplayNames
{
    public static string ForTarget(EntityReferenceType targetEntityType)
    {
        return targetEntityType switch
        {
            EntityReferenceType.TestSuite => "Test suite",
            EntityReferenceType.TestSuiteRevision => "Test suite revision",
            EntityReferenceType.TemplateSection => "Template section",
            EntityReferenceType.TestCaseTemplate => "Test case template",
            EntityReferenceType.TestStepTemplate => "Test check template",
            EntityReferenceType.TestSession => "Manual test session",
            EntityReferenceType.TestCaseResult => "Test case result",
            EntityReferenceType.TestStepResult => "Test check result",
            EntityReferenceType.BugReport => "Bug report",
            _ => targetEntityType.ToString()
        };
    }

    public static string ForFieldType(FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Text => "Short text",
            FieldType.LongText => "Long text",
            FieldType.Number => "Number",
            FieldType.Date => "Date",
            FieldType.DateTime => "Date and time",
            FieldType.Checkbox => "Checkbox",
            FieldType.SingleSelect => "Single select",
            FieldType.MultiSelect => "Multi select",
            FieldType.AttachmentReference => "Attachment reference",
            _ => fieldType.ToString()
        };
    }
}
