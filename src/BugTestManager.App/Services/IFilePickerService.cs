namespace BugTestManager.App.Services;

public interface IFilePickerService
{
    string? PickAttachmentFile();

    string? PickPdfReportSaveFile(string suggestedFileName);
}
