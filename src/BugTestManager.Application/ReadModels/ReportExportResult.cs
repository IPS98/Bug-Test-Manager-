namespace BugTestManager.Application.ReadModels;

public sealed record ReportExportResult(
    string OutputFilePath,
    DateTimeOffset ExportedAt);
