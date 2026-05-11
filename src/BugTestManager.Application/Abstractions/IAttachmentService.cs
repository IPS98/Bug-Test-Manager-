using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Abstractions;

public interface IAttachmentService
{
    IReadOnlyList<AttachmentItem> GetAttachments(EntityReferenceType entityType, Guid entityId);

    Guid AddAttachment(AddAttachmentRequest request);

    void DeleteAttachment(Guid attachmentId);
}
