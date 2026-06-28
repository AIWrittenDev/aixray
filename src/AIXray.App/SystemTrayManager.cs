using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AIXray.App;

/// <summary>
/// مدیریت آیکون system tray.
/// </summary>
public class SystemTrayManager : IDisposable
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private readonly Window _mainWindow;

    public SystemTrayManager(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Initialize()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "AIXray",
            Visible = false,
            Icon = System.Drawing.SystemIcons.Application,
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("نمایش", null, (_, _) => ShowMainWindow());
        menu.Items.Add("-");
        menu.Items.Add("خروج", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        _mainWindow.StateChanged += (_, _) =>
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.Hide();
                _notifyIcon.Visible = true;
            }
        };
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _notifyIcon.Visible = false;
        _mainWindow.Activate();
    }

    public void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
