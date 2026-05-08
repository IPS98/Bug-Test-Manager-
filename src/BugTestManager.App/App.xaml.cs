using System.Windows;
using BugTestManager.App.ViewModels;
using BugTestManager.App.Views;
using BugTestManager.Application.Abstractions;
using BugTestManager.Infrastructure.UserContext;
using Microsoft.Extensions.DependencyInjection;

namespace BugTestManager.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IUserContext, WindowsUserContext>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
