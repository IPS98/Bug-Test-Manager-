using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;

namespace BugTestManager.Application.Abstractions;

public interface IReportExportService
{
    ReportExportResult ExportTestSessionReport(ExportTestSessionReportRequest request);
}
