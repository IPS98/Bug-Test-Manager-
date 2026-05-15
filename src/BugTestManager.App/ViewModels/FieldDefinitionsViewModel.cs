using System.Collections.ObjectModel;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class FieldDefinitionsViewModel : ObservableObject
{
    private readonly ICustomFieldDefinitionService fieldDefinitionService;
    private readonly ITestSuiteCatalogService testSuiteCatalogService;
    private readonly IProjectContext projectContext;

    public FieldDefinitionsViewModel(
        ICustomFieldDefinitionService fieldDefinitionService,
        ITestSuiteCatalogService testSuiteCatalogService,
        IProjectContext projectContext)
    {
        this.fieldDefinitionService = fieldDefinitionService;
        this.testSuiteCatalogService = testSuiteCatalogService;
        this.projectContext = projectContext;
        Fields = [];
        ScopeOptions = [];
        EditScopeOptions = [];
        TargetEntityTypes =
        [
            new SelectionOption<EntityReferenceType>(EntityReferenceType.TestCaseResult, "Test case fields"),
            new SelectionOption<EntityReferenceType>(EntityReferenceType.TestStepResult, "Check fields"),
            new SelectionOption<EntityReferenceType>(EntityReferenceType.BugReport, "Bug fields")
        ];
        FieldTypes = Enum.GetValues<FieldType>()
            .Select(CreateFieldTypeOption)
            .ToList();
        SelectedTargetEntityType = TargetEntityTypes.Single(option => option.Value == EntityReferenceType.TestCaseResult);
        SelectedFieldType = FieldTypes.Single(option => option.Value == FieldType.Text);
        Refresh();
    }

    public ObservableCollection<FieldDefinitionItemViewModel> Fields { get; }

    public ObservableCollection<FieldScopeOption> ScopeOptions { get; }

    public ObservableCollection<FieldScopeOption> EditScopeOptions { get; }

    public IReadOnlyList<SelectionOption<EntityReferenceType>> TargetEntityTypes { get; }

    public IReadOnlyList<SelectionOption<FieldType>> FieldTypes { get; }

    [ObservableProperty]
    private SelectionOption<EntityReferenceType>? selectedTargetEntityType;

    [ObservableProperty]
    private SelectionOption<FieldType>? selectedFieldType;

    [ObservableProperty]
    private string newFieldName = string.Empty;

    [ObservableProperty]
    private bool newFieldIsRequired;

    [ObservableProperty]
    private int newFieldSortOrder;

    [ObservableProperty]
    private string newFieldOptionsText = string.Empty;

    [ObservableProperty]
    private FieldDefinitionItemViewModel? editingField;

    [ObservableProperty]
    private Visibility editFieldDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private SelectionOption<EntityReferenceType>? editTargetEntityType;

    [ObservableProperty]
    private SelectionOption<FieldType>? editFieldType;

    [ObservableProperty]
    private string editFieldName = string.Empty;

    [ObservableProperty]
    private bool editFieldIsRequired;

    [ObservableProperty]
    private int editFieldSortOrder;

    [ObservableProperty]
    private string editFieldOptionsText = string.Empty;

    [ObservableProperty]
    private FieldDefinitionItemViewModel? deletingField;

    [ObservableProperty]
    private Visibility deleteFieldDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string deleteFieldTitle = string.Empty;

    [ObservableProperty]
    private string deleteFieldWarning = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    partial void OnNewFieldNameChanged(string value)
    {
        CreateFieldCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditFieldNameChanged(string value)
    {
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditTargetEntityTypeChanged(SelectionOption<EntityReferenceType>? value)
    {
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditFieldTypeChanged(SelectionOption<FieldType>? value)
    {
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    public void Refresh()
    {
        LoadScopeOptions();
        LoadFields();
    }

    [RelayCommand(CanExecute = nameof(CanCreateField))]
    private void CreateField()
    {
        try
        {
            var createdFieldId = fieldDefinitionService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
                SelectedTargetEntityType!.Value,
                NewFieldName,
                SelectedFieldType!.Value,
                NewFieldIsRequired,
                NewFieldSortOrder,
                CustomFieldScopeType.Global,
                null,
                "Whole project",
                ParseOptions(NewFieldOptionsText),
                projectContext.CurrentProjectId,
                BuildSelectedScopeRequests(ScopeOptions)));

            NewFieldName = string.Empty;
            NewFieldIsRequired = false;
            NewFieldSortOrder = 0;
            NewFieldOptionsText = string.Empty;
            LoadFields(createdFieldId);
            StatusMessage = "Field definition created.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ShowEditFieldDialog(FieldDefinitionItemViewModel? field)
    {
        if (field is null)
        {
            return;
        }

        EditingField = field;
        EditTargetEntityType = TargetEntityTypes.FirstOrDefault(option => option.Value == field.TargetEntityType)
            ?? TargetEntityTypes.FirstOrDefault();
        EditFieldType = FieldTypes.Single(option => option.Value == field.FieldType);
        LoadEditScopeOptions(field.Scopes);
        EditFieldName = field.Name;
        EditFieldIsRequired = field.IsRequired;
        EditFieldSortOrder = field.SortOrder;
        EditFieldOptionsText = string.Join(Environment.NewLine, field.Options);
        EditFieldDialogVisibility = Visibility.Visible;
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CloseEditFieldDialog()
    {
        EditFieldDialogVisibility = Visibility.Collapsed;
        EditingField = null;
        EditFieldName = string.Empty;
        EditFieldIsRequired = false;
        EditFieldSortOrder = 0;
        EditFieldOptionsText = string.Empty;
        EditTargetEntityType = TargetEntityTypes.FirstOrDefault();
        EditFieldType = FieldTypes.FirstOrDefault();
        EditScopeOptions.Clear();
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUpdateField))]
    private void UpdateField()
    {
        if (EditingField is null || EditTargetEntityType is null || EditFieldType is null)
        {
            return;
        }

        try
        {
            fieldDefinitionService.UpdateDefinition(new UpdateCustomFieldDefinitionRequest(
                EditingField.Id,
                EditTargetEntityType.Value,
                EditFieldName,
                EditFieldType.Value,
                EditFieldIsRequired,
                EditFieldSortOrder,
                CustomFieldScopeType.Global,
                null,
                "Whole project",
                ParseOptions(EditFieldOptionsText),
                BuildSelectedScopeRequests(EditScopeOptions)));

            var fieldId = EditingField.Id;
            CloseEditFieldDialog();
            LoadFields(fieldId);
            StatusMessage = "Field definition updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ArchiveField(FieldDefinitionItemViewModel? field)
    {
        if (field is null)
        {
            return;
        }

        try
        {
            fieldDefinitionService.ArchiveDefinition(field.Id);
            LoadFields();
            StatusMessage = $"Field '{field.Name}' archived.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ShowDeleteFieldDialog(FieldDefinitionItemViewModel? field)
    {
        if (field is null)
        {
            return;
        }

        DeletingField = field;
        DeleteFieldTitle = $"Delete field: {field.Name}";
        DeleteFieldWarning = "This will permanently delete the field definition and all saved values that use it.";
        DeleteFieldDialogVisibility = Visibility.Visible;
    }

    [RelayCommand]
    private void CloseDeleteFieldDialog()
    {
        DeleteFieldDialogVisibility = Visibility.Collapsed;
        DeletingField = null;
        DeleteFieldTitle = string.Empty;
        DeleteFieldWarning = string.Empty;
    }

    [RelayCommand]
    private void ConfirmDeleteField()
    {
        if (DeletingField is null)
        {
            return;
        }

        try
        {
            var deletedFieldName = DeletingField.Name;
            fieldDefinitionService.DeleteDefinition(DeletingField.Id);
            CloseDeleteFieldDialog();
            LoadFields();
            StatusMessage = $"Field '{deletedFieldName}' deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private bool CanCreateField()
    {
        return SelectedTargetEntityType is not null
            && SelectedFieldType is not null
            && ScopeOptions.Any(option => option.IsSelected)
            && !string.IsNullOrWhiteSpace(NewFieldName);
    }

    private bool CanUpdateField()
    {
        return EditingField is not null
            && EditTargetEntityType is not null
            && EditFieldType is not null
            && EditScopeOptions.Any(option => option.IsSelected)
            && !string.IsNullOrWhiteSpace(EditFieldName);
    }

    private void LoadScopeOptions()
    {
        var options = BuildScopeOptions();

        ScopeOptions.Clear();
        foreach (var option in options)
        {
            option.PropertyChanged += (_, _) => CreateFieldCommand.NotifyCanExecuteChanged();
            ScopeOptions.Add(option);
        }
    }

    private void LoadFields(Guid? selectedFieldId = null)
    {
        var fields = fieldDefinitionService.GetDefinitions(projectContext.CurrentProjectId)
            .Select(MapField)
            .ToList();

        Fields.Clear();
        foreach (var field in fields)
        {
            Fields.Add(field);
        }
    }

    private static IReadOnlyCollection<string> ParseOptions(string optionsText)
    {
        return optionsText
            .Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .ToList();
    }

    private static FieldDefinitionItemViewModel MapField(CustomFieldDefinitionItem field)
    {
        return new FieldDefinitionItemViewModel(
            field.Id,
            field.TargetEntityType,
            field.Name,
            field.FieldType,
            field.IsRequired,
            field.SortOrder,
            field.ScopeType,
            field.ScopeEntityId,
            field.ScopeDisplayName,
            field.IsActive,
            field.Options,
            field.Scopes
                .Select(scope => new FieldScopeOption(
                    scope.ScopeType,
                    scope.ScopeEntityId,
                    scope.DisplayName,
                    isSelected: true))
                .ToList());
    }

    private IReadOnlyList<FieldScopeOption> BuildScopeOptions()
    {
        var options = new List<FieldScopeOption>
        {
            new(CustomFieldScopeType.Global, null, "Whole project", isSelected: true)
        };

        foreach (var testSuite in testSuiteCatalogService.GetCatalog(projectContext.CurrentProjectId))
        {
            options.Add(new FieldScopeOption(
                CustomFieldScopeType.TestSuite,
                testSuite.Id,
                $"Whole test suite: {testSuite.Name}"));

            foreach (var revision in testSuite.Revisions)
            {
                foreach (var section in revision.Sections)
                {
                    options.Add(new FieldScopeOption(
                        CustomFieldScopeType.TemplateSection,
                        section.Id,
                        $"Section: {testSuite.Name} / {section.Name}"));

                    foreach (var testCase in section.TestCases)
                    {
                        options.Add(new FieldScopeOption(
                            CustomFieldScopeType.TestCaseTemplate,
                            testCase.Id,
                            $"Test case: {testSuite.Name} / {section.Name} / {testCase.Title}"));
                    }
                }
            }
        }

        return options;
    }

    private void LoadEditScopeOptions(IReadOnlyList<FieldScopeOption> selectedScopes)
    {
        EditScopeOptions.Clear();
        foreach (var option in BuildScopeOptions())
        {
            option.IsSelected = selectedScopes.Any(scope =>
                scope.ScopeType == option.ScopeType && scope.ScopeEntityId == option.ScopeEntityId);
            option.PropertyChanged += (_, _) => UpdateFieldCommand.NotifyCanExecuteChanged();
            EditScopeOptions.Add(option);
        }
    }

    private static IReadOnlyCollection<CustomFieldDefinitionScopeRequest> BuildSelectedScopeRequests(
        IEnumerable<FieldScopeOption> options)
    {
        return options
            .Where(option => option.IsSelected)
            .Select(option => new CustomFieldDefinitionScopeRequest(
                option.ScopeType,
                option.ScopeEntityId,
                option.DisplayName))
            .ToList();
    }

    private static SelectionOption<EntityReferenceType> CreateTargetOption(EntityReferenceType targetEntityType)
    {
        return new SelectionOption<EntityReferenceType>(
            targetEntityType,
            FieldDisplayNames.ForTarget(targetEntityType));
    }

    private static SelectionOption<FieldType> CreateFieldTypeOption(FieldType fieldType)
    {
        return new SelectionOption<FieldType>(
            fieldType,
            FieldDisplayNames.ForFieldType(fieldType));
    }
}
