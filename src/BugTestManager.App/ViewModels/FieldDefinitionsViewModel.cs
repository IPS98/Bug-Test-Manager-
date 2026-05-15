using System.Collections.ObjectModel;
using System.Windows;
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

    public FieldDefinitionsViewModel(
        ICustomFieldDefinitionService fieldDefinitionService,
        ITestSuiteCatalogService testSuiteCatalogService)
    {
        this.fieldDefinitionService = fieldDefinitionService;
        this.testSuiteCatalogService = testSuiteCatalogService;
        Fields = [];
        ScopeOptions = [];
        TargetEntityTypes =
        [
            CreateTargetOption(EntityReferenceType.TestSuite),
            CreateTargetOption(EntityReferenceType.TestSuiteRevision),
            CreateTargetOption(EntityReferenceType.TemplateSection),
            CreateTargetOption(EntityReferenceType.TestCaseTemplate),
            CreateTargetOption(EntityReferenceType.TestStepTemplate),
            CreateTargetOption(EntityReferenceType.TestSession),
            CreateTargetOption(EntityReferenceType.TestCaseResult),
            CreateTargetOption(EntityReferenceType.TestStepResult),
            CreateTargetOption(EntityReferenceType.BugReport)
        ];
        FieldTypes = Enum.GetValues<FieldType>()
            .Select(CreateFieldTypeOption)
            .ToList();
        SelectedTargetEntityType = TargetEntityTypes.Single(option => option.Value == EntityReferenceType.TestCaseTemplate);
        SelectedFieldType = FieldTypes.Single(option => option.Value == FieldType.Text);
        Refresh();
    }

    public ObservableCollection<FieldDefinitionItemViewModel> Fields { get; }

    public ObservableCollection<FieldScopeOption> ScopeOptions { get; }

    public IReadOnlyList<SelectionOption<EntityReferenceType>> TargetEntityTypes { get; }

    public IReadOnlyList<SelectionOption<FieldType>> FieldTypes { get; }

    [ObservableProperty]
    private SelectionOption<EntityReferenceType>? selectedTargetEntityType;

    [ObservableProperty]
    private SelectionOption<FieldType>? selectedFieldType;

    [ObservableProperty]
    private FieldScopeOption? selectedScopeOption;

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
    private FieldScopeOption? editScopeOption;

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

    partial void OnSelectedScopeOptionChanged(FieldScopeOption? value)
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

    partial void OnEditScopeOptionChanged(FieldScopeOption? value)
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
                SelectedScopeOption!.ScopeType,
                SelectedScopeOption.ScopeEntityId,
                SelectedScopeOption.DisplayName,
                ParseOptions(NewFieldOptionsText)));

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
        EditTargetEntityType = TargetEntityTypes.Single(option => option.Value == field.TargetEntityType);
        EditFieldType = FieldTypes.Single(option => option.Value == field.FieldType);
        EditScopeOption = ScopeOptions.FirstOrDefault(option =>
            option.ScopeType == field.ScopeType && option.ScopeEntityId == field.ScopeEntityId)
            ?? ScopeOptions.FirstOrDefault();
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
        EditScopeOption = ScopeOptions.FirstOrDefault();
        UpdateFieldCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUpdateField))]
    private void UpdateField()
    {
        if (EditingField is null || EditTargetEntityType is null || EditFieldType is null || EditScopeOption is null)
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
                EditScopeOption.ScopeType,
                EditScopeOption.ScopeEntityId,
                EditScopeOption.DisplayName,
                ParseOptions(EditFieldOptionsText)));

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
            && SelectedScopeOption is not null
            && !string.IsNullOrWhiteSpace(NewFieldName);
    }

    private bool CanUpdateField()
    {
        return EditingField is not null
            && EditTargetEntityType is not null
            && EditFieldType is not null
            && EditScopeOption is not null
            && !string.IsNullOrWhiteSpace(EditFieldName);
    }

    private void LoadScopeOptions()
    {
        var selectedScope = SelectedScopeOption;
        var options = BuildScopeOptions();

        ScopeOptions.Clear();
        foreach (var option in options)
        {
            ScopeOptions.Add(option);
        }

        SelectedScopeOption = selectedScope is null
            ? ScopeOptions.FirstOrDefault()
            : ScopeOptions.FirstOrDefault(option =>
                option.ScopeType == selectedScope.ScopeType && option.ScopeEntityId == selectedScope.ScopeEntityId)
                ?? ScopeOptions.FirstOrDefault();
    }

    private void LoadFields(Guid? selectedFieldId = null)
    {
        var fields = fieldDefinitionService.GetDefinitions()
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
            field.Options);
    }

    private IReadOnlyList<FieldScopeOption> BuildScopeOptions()
    {
        var options = new List<FieldScopeOption>
        {
            new(CustomFieldScopeType.Global, null, "All matching items")
        };

        foreach (var testSuite in testSuiteCatalogService.GetCatalog())
        {
            options.Add(new FieldScopeOption(
                CustomFieldScopeType.TestSuite,
                testSuite.Id,
                $"Test suite: {testSuite.Name}"));

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
