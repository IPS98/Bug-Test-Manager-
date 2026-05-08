using BugTestManager.App.ViewModels;
using MahApps.Metro.Controls;

namespace BugTestManager.App.Views;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
