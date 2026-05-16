using System.Collections.ObjectModel;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly ITestSessionService testSessionService;
    private readonly IReportDataService reportDataService;
    private readonly IProjectContext projectContext;

    public ReportsViewModel(
        ITestSessionService testSessionService,
        IReportDataService reportDataService,
        IProjectContext projectContext)
    {
        this.testSessionService = testSessionService;
        this.reportDataService = reportDataService;
        this.projectContext = projectContext;
        Sessions = [];
        StatusMessage = "Select a test session and prepare report data.";
        Refresh();
    }

    public ObservableCollection<TestSessionSummaryViewModel> Sessions { get; }

    public Visibility ReportVisibility => SelectedReport is null
        ? Visibility.Collapsed
        : Visibility.Visible;

    [ObservableProperty]
    private TestSessionSummaryViewModel? selectedSession;

    [ObservableProperty]
    private TestSessionReportItem? selectedReport;

    [ObservableProperty]
    private string statusMessage;

    partial void OnSelectedSessionChanged(TestSessionSummaryViewModel? value)
    {
        LoadReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedReportChanged(TestSessionReportItem? value)
    {
        OnPropertyChanged(nameof(ReportVisibility));
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

        SelectedSession = selectedSessionId is null
            ? Sessions.FirstOrDefault()
            : Sessions.FirstOrDefault(session => session.Id == selectedSessionId) ?? Sessions.FirstOrDefault();
        LoadReportCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanLoadReport))]
    private void LoadReport()
    {
        if (SelectedSession is null)
        {
            return;
        }

        try
        {
            SelectedReport = reportDataService.GetTestSessionReport(
                SelectedSession.Id,
                projectContext.CurrentProjectId);
            StatusMessage = "Report data prepared. PDF rendering will use this data model.";
        }
        catch (Exception ex)
        {
            SelectedReport = null;
            StatusMessage = ex.Message;
        }
    }

    private bool CanLoadReport()
    {
        return SelectedSession is not null;
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
