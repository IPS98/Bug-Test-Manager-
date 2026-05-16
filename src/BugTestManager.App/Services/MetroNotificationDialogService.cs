using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace BugTestManager.App.Services;

public sealed class MetroNotificationDialogService : INotificationDialogService
{
    public void ShowInfo(string title, string message)
    {
        var application = System.Windows.Application.Current;
        if (application?.Dispatcher is null)
        {
            return;
        }

        if (application.Dispatcher.CheckAccess())
        {
            _ = ShowInfoAsync(title, message);
            return;
        }

        application.Dispatcher.InvokeAsync(() => _ = ShowInfoAsync(title, message));
    }

    private static async Task ShowInfoAsync(string title, string message)
    {
        if (System.Windows.Application.Current.MainWindow is not MetroWindow metroWindow)
        {
            return;
        }

        var settings = new MetroDialogSettings
        {
            AffirmativeButtonText = "OK",
            AnimateShow = true,
            AnimateHide = true,
            ColorScheme = MetroDialogColorScheme.Theme
        };

        await metroWindow.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings);
    }
}
