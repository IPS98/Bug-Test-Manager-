namespace BugTestManager.Infrastructure.Data;

public static class DatabasePaths
{
    public static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "BugTestManager");

        return Path.Combine(directory, "BugTestManager.db");
    }

    public static string GetDefaultAttachmentRootPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(appData, "BugTestManager", "Attachments");
    }
}
