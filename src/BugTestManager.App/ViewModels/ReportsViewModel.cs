using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly ITestSessionService testSessionService;
    private readonly IReportDataService reportDataService;
    private readonly IReportExportService reportExportService;
    private readonly IFilePickerService filePickerService;
    private readonly IFileLauncherService fileLauncherService;
    private readonly IErrorDialogService errorDialogService;
    private readonly IProjectContext projectContext;
    private readonly IUserContext userContext;

    public ReportsViewModel(
        ITestSessionService testSessionService,
        IReportDataService reportDataService,
        IReportExportService reportExportService,
        IFilePickerService filePickerService,
        IFileLauncherService fileLauncherService,
        IErrorDialogService errorDialogService,
        IProjectContext projectContext,
        IUserContext userContext)
    {
        this.testSessionService = testSessionService;
        this.reportDataService = reportDataService;
        this.reportExportService = reportExportService;
        this.filePickerService = filePickerService;
        this.fileLauncherService = fileLauncherService;
        this.errorDialogService = errorDialogService;
        this.projectContext = projectContext;
        this.userContext = userContext;
        Sessions = [];
        StatusMessage = "Select a test session and prepare report data.";
        Refresh();
    }

    public ObservableCollection<TestSessionSummaryViewModel> Sessions { get; }

    public Visibility ReportVisibility => SelectedReport is null
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility ExportedReportVisibility => string.IsNullOrWhiteSpace(ExportedReportPath)
        ? Visibility.Collapsed
        : Visibility.Visible;

    [ObservableProperty]
    private TestSessionSummaryViewModel? selectedSession;

    [ObservableProperty]
    private TestSessionReportItem? selectedReport;

    [ObservableProperty]
    private string statusMessage;

    [ObservableProperty]
    private string exportedReportPath = string.Empty;

    partial void OnSelectedSessionChanged(TestSessionSummaryViewModel? value)
    {
        SelectedReport = null;
        ExportedReportPath = string.Empty;
        LoadReportCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedReportChanged(TestSessionReportItem? value)
    {
        OnPropertyChanged(nameof(ReportVisibility));
        ExportPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnExportedReportPathChanged(string value)
    {
        OnPropertyChanged(nameof(ExportedReportVisibility));
        OpenExportedReportCommand.NotifyCanExecuteChanged();
    }

    public void Refresh()
    {
        var selectedSessionId = SelectedSession?.Id;
        var sessions = testSessionService.GetSessions(projectContext.CurrentProjectId)
            .Select(MapSession)
            .ToList();

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        var previousSelectedSession = SelectedSession;
        SelectedSession = selectedSessionId is null
            ? Sessions.FirstOrDefault()
            : Sessions.FirstOrDefault(session => session.Id == selectedSessionId) ?? Sessions.FirstOrDefault();

        if (SelectedSession?.Id != previousSelectedSession?.Id)
        {
            SelectedReport = null;
            ExportedReportPath = string.Empty;
        }

        LoadReportCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanLoadReport))]
    private void LoadReport()
    {
        try
        {
            LoadSelectedReport();
        }
        catch (Exception ex)
        {
            SelectedReport = null;
            ExportedReportPath = string.Empty;
            StatusMessage = ex.Message;
            errorDialogService.ShowError("Report Preview Error", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportPdf))]
    private void ExportPdf()
    {
        if (SelectedSession is null)
        {
            return;
        }

        try
        {
            LoadSelectedReport();
            if (SelectedReport is null)
            {
                return;
            }

            var outputFilePath = filePickerService.PickPdfReportSaveFile(BuildReportFileName(SelectedReport));
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                StatusMessage = "PDF export cancelled.";
                return;
            }

            var result = reportExportService.ExportTestSessionReport(new ExportTestSessionReportRequest(
                SelectedReport,
                outputFilePath,
                userContext.UserName));

            ExportedReportPath = result.OutputFilePath;
            StatusMessage = $"PDF report exported: {result.OutputFilePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            errorDialogService.ShowError("Report Export Error", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenExportedReport))]
    private void OpenExportedReport()
    {
        if (string.IsNullOrWhiteSpace(ExportedReportPath))
        {
            return;
        }

        try
        {
            fileLauncherService.OpenFile(ExportedReportPath);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private bool CanLoadReport()
    {
        return SelectedSession is not null;
    }

    private bool CanExportPdf()
    {
        return SelectedSession is not null;
    }

    private bool CanOpenExportedReport()
    {
        return !string.IsNullOrWhiteSpace(ExportedReportPath);
    }

    private void LoadSelectedReport()
    {
        if (SelectedSession is null)
        {
            return;
        }

        SelectedReport = reportDataService.GetTestSessionReport(
            SelectedSession.Id,
            projectContext.CurrentProjectId);
        StatusMessage = "Report data prepared.";
    }

    private static string BuildReportFileName(TestSessionReportItem report)
    {
        var name = string.IsNullOrWhiteSpace(report.SessionName)
            ? "test-session-report"
            : report.SessionName;
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Select(character =>
            invalidCharacters.Contains(character) ? '-' : character).ToArray());

        return $"{safeName}-report.pdf";
    }

    private static TestSessionSummaryViewModel MapSession(TestSessionSummaryItem session)
    {
        return new TestSessionSummaryViewModel(
            session.Id,
            session.TestSuiteId,
            session.Name,
            session.TestSuiteName,
            session.TestSuiteRevisionName,
            session.TestedVersion,
            session.BuildNumber,
            session.CreatedBy,
            session.CreatedAt,
            session.SectionCount,
            session.TestCaseCount,
            session.StepCount);
    }
}
