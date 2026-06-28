using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class SettingsDialog : FluentWindow
{
    public ConnectionMode SelectedMode { get; private set; }
    public bool AutoConnect { get; private set; }

    public SettingsDialog(ConnectionMode currentMode, bool autoConnect)
    {
        InitializeComponent();

        ModeCombo.SelectedIndex = currentMode switch
        {
            ConnectionMode.SystemProxy => 0,
            ConnectionMode.Tun => 1,
            ConnectionMode.Direct => 2,
            _ => 0,
        };
        AutoConnectCheck.IsChecked = autoConnect;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        SelectedMode = ModeCombo.SelectedIndex switch
        {
            0 => ConnectionMode.SystemProxy,
            1 => ConnectionMode.Tun,
            2 => ConnectionMode.Direct,
            _ => ConnectionMode.SystemProxy,
        };
        AutoConnect = AutoConnectCheck.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
