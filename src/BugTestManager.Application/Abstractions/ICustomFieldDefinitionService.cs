using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;

namespace BugTestManager.Application.Abstractions;

public interface ICustomFieldDefinitionService
{
    IReadOnlyList<CustomFieldDefinitionItem> GetDefinitions();

    Guid CreateDefinition(CreateCustomFieldDefinitionRequest request);

    void UpdateDefinition(UpdateCustomFieldDefinitionRequest request);

    void ArchiveDefinition(Guid fieldDefinitionId);

    void DeleteDefinition(Guid fieldDefinitionId);
}
