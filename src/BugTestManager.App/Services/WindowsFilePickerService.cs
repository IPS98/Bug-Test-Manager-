using Microsoft.Win32;

namespace BugTestManager.App.Services;

public sealed class WindowsFilePickerService : IFilePickerService
{
    public string? PickAttachmentFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Attachment",
            Filter = "Evidence files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.pdf;*.txt;*.log;*.csv;*.xlsx;*.docx|All files|*.*",
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
