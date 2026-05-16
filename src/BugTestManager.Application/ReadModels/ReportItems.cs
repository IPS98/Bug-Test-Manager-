using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record TestSessionReportItem(
    Guid SessionId,
    string SessionName,
    string ProjectName,
    string TestSuiteName,
    string? TestSuiteRevisionName,
    string TestedVersion,
    string BuildNumber,
    string Notes,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    ReportStatusSummaryItem Summary,
    IReadOnlyList<ReportSectionItem> Sections,
    IReadOnlyList<ReportBugItem> LinkedBugs)
{
    public string CreatedDateDisplay => ReportDisplayFormatter.FormatDate(CreatedAt);
}

public sealed record ReportStatusSummaryItem(
    int Total,
    int Passed,
    int Failed,
    int Blocked,
    int NotTested,
    int NotApplicable);

public sealed record ReportSectionItem(
    Guid Id,
    string Name,
    string Category,
    int SortOrder,
    IReadOnlyList<ReportTestCaseItem> TestCases);

public sealed record ReportTestCaseItem(
    Guid Id,
    string Title,
    string ExpectedResult,
    int SortOrder,
    TestResultStatus Status,
    DateTimeOffset? LastStatusChangedAt,
    string Comment,
    IReadOnlyList<ReportCustomFieldItem> CustomFields,
    IReadOnlyList<ReportAttachmentItem> Attachments,
    IReadOnlyList<ReportCheckItem> Checks)
{
    public string LastStatusChangedDateDisplay => ReportDisplayFormatter.FormatOptionalDate(LastStatusChangedAt);
}

public sealed record ReportCheckItem(
    Guid Id,
    string Text,
    string ExpectedResult,
    int SortOrder,
    TestResultStatus Status,
    DateTimeOffset? LastStatusChangedAt,
    string Comment,
    IReadOnlyList<ReportCustomFieldItem> CustomFields,
    IReadOnlyList<ReportAttachmentItem> Attachments)
{
    public string LastStatusChangedDateDisplay => ReportDisplayFormatter.FormatOptionalDate(LastStatusChangedAt);
}

public sealed record ReportCustomFieldItem(
    string Name,
    FieldType FieldType,
    string Value,
    string UpdatedBy,
    DateTimeOffset UpdatedAt)
{
    public string DisplayValue => ReportDisplayFormatter.FormatCustomFieldValue(FieldType, Value);

    public string UpdatedDateDisplay => ReportDisplayFormatter.FormatDate(UpdatedAt);
}

public sealed record ReportAttachmentItem(
    Guid Id,
    string OriginalFileName,
    string AbsolutePath,
    string ContentType,
    long SizeBytes,
    string UploadedBy,
    DateTimeOffset UploadedAt)
{
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public string UploadedDateDisplay => ReportDisplayFormatter.FormatDate(UploadedAt);
}

public sealed record ReportBugItem(
    Guid Id,
    string Title,
    string Status,
    string Severity,
    string Priority,
    string LinkedEntityDisplayName,
    string FoundInVersion,
    string BuildNumber,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string UpdatedBy,
    DateTimeOffset UpdatedAt)
{
    public string CreatedDateDisplay => ReportDisplayFormatter.FormatDate(CreatedAt);

    public string UpdatedDateDisplay => ReportDisplayFormatter.FormatDate(UpdatedAt);
}
