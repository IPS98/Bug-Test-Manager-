using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Abstractions;

public interface ICustomFieldValueService
{
    IReadOnlyList<CustomFieldValueItem> GetValues(
        EntityReferenceType entityType,
        Guid entityId,
        IReadOnlyCollection<CustomFieldValueScopeItem> scopes,
        Guid? projectId = null);

    void SaveValue(SaveCustomFieldValueRequest request);
}
