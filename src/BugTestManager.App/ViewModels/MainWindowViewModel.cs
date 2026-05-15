using System.Collections.ObjectModel;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IProjectService projectService;
    private readonly IProjectContext projectContext;
    private readonly TestSuitesViewModel testSuitesViewModel;
    private readonly FieldDefinitionsViewModel fieldDefinitionsViewModel;
    private readonly TestSessionsViewModel testSessionsViewModel;
    private readonly BugReportsViewModel bugReportsViewModel;

    public MainWindowViewModel(
        IUserContext userContext,
        IProjectService projectService,
        IProjectContext projectContext,
        TestSuitesViewModel testSuitesViewModel,
        FieldDefinitionsViewModel fieldDefinitionsViewModel,
        TestSessionsViewModel testSessionsViewModel,
        BugReportsViewModel bugReportsViewModel)
    {
        this.projectService = projectService;
        this.projectContext = projectContext;
        this.testSuitesViewModel = testSuitesViewModel;
        this.fieldDefinitionsViewModel = fieldDefinitionsViewModel;
        this.testSessionsViewModel = testSessionsViewModel;
        this.bugReportsViewModel = bugReportsViewModel;
        CurrentUserDisplay = $"Signed in as {userContext.UserName} ({userContext.Role})";
        Projects = [];

        Modules =
        [
            new NavigationItemViewModel(AppPageKey.Templates, "Templates", "Reusable test suites, revisions, sections, cases, and checks."),
            new NavigationItemViewModel(AppPageKey.Fields, "Fields", "User-defined fields for tests, bugs, sessions, and reports."),
            new NavigationItemViewModel(AppPageKey.TestSessions, "Test Sessions", "Manual test reports, statuses, photos, comments, and dates."),
            new NavigationItemViewModel(AppPageKey.Bugs, "Bugs", "Bug reports, developer comments, status changes, and retesting."),
            new NavigationItemViewModel(AppPageKey.Reports, "Reports", "Readable PDF reports for versions, revisions, tests, and bugs.")
        ];

        LoadProjects();
        SelectModule(Modules[0]);
    }

    public string Title => "Bug & Test Manager";

    public string CurrentUserDisplay { get; }

    public ObservableCollection<NavigationItemViewModel> Modules { get; }

    public ObservableCollection<ProjectOptionViewModel> Projects { get; }

    [ObservableProperty]
    private string currentPageTitle = string.Empty;

    [ObservableProperty]
    private string currentPageDescription = string.Empty;

    [ObservableProperty]
    private object? currentPage;

    [ObservableProperty]
    private ProjectOptionViewModel? selectedProject;

    [ObservableProperty]
    private string newProjectName = string.Empty;

    [ObservableProperty]
    private string projectStatusMessage = string.Empty;

    [ObservableProperty]
    private Visibility projectDeleteDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string projectDeleteTitle = string.Empty;

    [ObservableProperty]
    private string projectDeleteWarning = string.Empty;

    private Guid pendingDeleteProjectId;

    partial void OnSelectedProjectChanged(ProjectOptionViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        projectContext.CurrentProjectId = value.Id;
        ShowDeleteProjectDialogCommand.NotifyCanExecuteChanged();
        RefreshCurrentPage();
    }

    partial void OnNewProjectNameChanged(string value)
    {
        CreateProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectModule(NavigationItemViewModel module)
    {
        foreach (var navigationItem in Modules)
        {
            navigationItem.IsSelected = navigationItem == module;
        }

        CurrentPageTitle = module.Name;
        CurrentPageDescription = module.Description;
        CurrentPage = module.PageKey switch
        {
            AppPageKey.Templates => testSuitesViewModel,
            AppPageKey.Fields => GetFieldDefinitionsViewModel(),
            AppPageKey.TestSessions => GetTestSessionsViewModel(),
            AppPageKey.Bugs => GetBugReportsViewModel(),
            _ => new PlaceholderPageViewModel(module.Name, "This page will be implemented after the template browser is stable.")
        };
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private void CreateProject()
    {
        try
        {
            var projectId = projectService.CreateProject(new CreateProjectRequest(NewProjectName));
            NewProjectName = string.Empty;
            LoadProjects(projectId);
            ProjectStatusMessage = "Project created.";
        }
        catch (Exception ex)
        {
            ProjectStatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteProjectDialog))]
    private void ShowDeleteProjectDialog()
    {
        if (SelectedProject is null)
        {
            return;
        }

        pendingDeleteProjectId = SelectedProject.Id;
        ProjectDeleteTitle = $"Delete project: {SelectedProject.Name}";
        ProjectDeleteWarning = "This will permanently delete this project with its templates, sessions, bugs, fields, attachments, and discussions.";
        ProjectStatusMessage = "This action cannot be undone.";
        ProjectDeleteDialogVisibility = Visibility.Visible;
        ConfirmDeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CloseDeleteProjectDialog()
    {
        ProjectDeleteDialogVisibility = Visibility.Collapsed;
        pendingDeleteProjectId = Guid.Empty;
        ProjectDeleteTitle = string.Empty;
        ProjectDeleteWarning = string.Empty;
        ConfirmDeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmDeleteProject))]
    private void ConfirmDeleteProject()
    {
        if (pendingDeleteProjectId == Guid.Empty)
        {
            return;
        }

        try
        {
            var deletedProjectId = pendingDeleteProjectId;
            projectService.DeleteProject(deletedProjectId);
            CloseDeleteProjectDialog();

            var nextProjectId = Projects.FirstOrDefault(project => project.Id != deletedProjectId)?.Id;
            projectContext.CurrentProjectId = nextProjectId ?? ProjectDefaults.DefaultProjectId;
            LoadProjects(projectContext.CurrentProjectId);
            RefreshCurrentPage();
            ProjectStatusMessage = "Project deleted.";
        }
        catch (Exception ex)
        {
            ProjectStatusMessage = ex.Message;
        }
    }

    private FieldDefinitionsViewModel GetFieldDefinitionsViewModel()
    {
        fieldDefinitionsViewModel.Refresh();
        return fieldDefinitionsViewModel;
    }

    private TestSessionsViewModel GetTestSessionsViewModel()
    {
        testSessionsViewModel.Refresh();
        return testSessionsViewModel;
    }

    private BugReportsViewModel GetBugReportsViewModel()
    {
        bugReportsViewModel.Refresh();
        return bugReportsViewModel;
    }

    private bool CanCreateProject()
    {
        return !string.IsNullOrWhiteSpace(NewProjectName);
    }

    private bool CanShowDeleteProjectDialog()
    {
        return SelectedProject is not null && Projects.Count > 1;
    }

    private bool CanConfirmDeleteProject()
    {
        return pendingDeleteProjectId != Guid.Empty;
    }

    private void LoadProjects(Guid? selectedProjectId = null)
    {
        var projectId = selectedProjectId ?? projectContext.CurrentProjectId;
        var projects = projectService.GetProjects()
            .Select(MapProject)
            .ToList();

        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }

        SelectedProject = Projects.FirstOrDefault(project => project.Id == projectId)
            ?? Projects.FirstOrDefault(project => project.Id == ProjectDefaults.DefaultProjectId)
            ?? Projects.FirstOrDefault();
    }

    private void RefreshCurrentPage()
    {
        switch (CurrentPage)
        {
            case TestSuitesViewModel:
                testSuitesViewModel.Refresh();
                break;
            case FieldDefinitionsViewModel:
                fieldDefinitionsViewModel.Refresh();
                break;
            case TestSessionsViewModel:
                testSessionsViewModel.Refresh();
                break;
            case BugReportsViewModel:
                bugReportsViewModel.Refresh();
                break;
        }
    }

    private static ProjectOptionViewModel MapProject(ProjectItem project)
    {
        return new ProjectOptionViewModel(
            project.Id,
            project.Name,
            project.Description);
    }
}
