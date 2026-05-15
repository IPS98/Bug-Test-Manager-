using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.App.ViewModels;
using BugTestManager.App.Views;
using BugTestManager.Application.Abstractions;
using BugTestManager.Infrastructure;
using BugTestManager.Infrastructure.Data;
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
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();
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
        services.AddSingleton<IFilePickerService, WindowsFilePickerService>();
        services.AddSingleton<IFileLauncherService, WindowsFileLauncherService>();
        services.AddSingleton<IErrorDialogService, MetroErrorDialogService>();
        services.AddSingleton<IProjectContext, ProjectContext>();
        services.AddInfrastructure();
        services.AddSingleton<TestSuitesViewModel>();
        services.AddSingleton<FieldDefinitionsViewModel>();
        services.AddSingleton<TestSessionsViewModel>();
        services.AddSingleton<BugReportsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
