using BugTestManager.Application.ReadModels;

namespace BugTestManager.Application.Requests;

public sealed record ExportTestSessionReportRequest(
    TestSessionReportItem Report,
    string OutputFilePath,
    string GeneratedBy);
