using System.Windows;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class MainWindow : FluentWindow
{
    private SystemTrayManager? _trayManager;

    public MainWindow()
    {
        InitializeComponent();

        if (App.Services != null)
        {
            DataContext = App.Services.GetService(typeof(MainViewModel));
        }

        _trayManager = new SystemTrayManager(this);
        _trayManager.Initialize();

        var args = Environment.GetCommandLineArgs();
        if (args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            WindowState = WindowState.Minimized;
            Hide();
        }

        Loaded += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                _ = vm.InitializeCommand.ExecuteAsync(null);
            }
        };
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        WindowState = WindowState.Minimized;
        Hide();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        _trayManager?.ExitApplication();
    }
}
