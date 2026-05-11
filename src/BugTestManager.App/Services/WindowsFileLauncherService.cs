using System.Diagnostics;
using System.IO;

namespace BugTestManager.App.Services;

public sealed class WindowsFileLauncherService : IFileLauncherService
{
    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Attachment file was not found.", filePath);
        }

        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        });
    }
}
