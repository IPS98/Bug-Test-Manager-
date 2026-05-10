using System.Collections.ObjectModel;
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
    private string statusMessage = "Ready";

    partial void OnNewFieldNameChanged(string value)
    {
        CreateFieldCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedScopeOptionChanged(FieldScopeOption? value)
    {
        CreateFieldCommand.NotifyCanExecuteChanged();
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

    private bool CanCreateField()
    {
        return SelectedTargetEntityType is not null
            && SelectedFieldType is not null
            && SelectedScopeOption is not null
            && !string.IsNullOrWhiteSpace(NewFieldName);
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
