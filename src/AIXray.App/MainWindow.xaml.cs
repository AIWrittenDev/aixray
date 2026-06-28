using System.Windows;
using System.Windows.Input;
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

        // پشتیبانی از Ctrl+V برای پیست از کلیپ‌بورد
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel vm)
                {
                    _ = vm.ImportFromClipboardCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
        };

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
