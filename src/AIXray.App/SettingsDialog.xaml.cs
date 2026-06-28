using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class SettingsDialog : FluentWindow
{
    public ConnectionMode SelectedMode { get; private set; }
    public bool AutoConnect { get; private set; }
    public LogLevel SelectedLogLevel { get; private set; }
    public int LocalPort { get; private set; }
    public AppLanguage SelectedLanguage { get; private set; }

    public SettingsDialog(AppSettings settings)
    {
        InitializeComponent();

        ModeCombo.SelectedIndex = settings.Mode switch
        {
            ConnectionMode.SystemProxy => 0,
            ConnectionMode.Tun => 1,
            ConnectionMode.Direct => 2,
            _ => 0,
        };
        AutoConnectCheck.IsChecked = settings.AutoConnect;

        LogLevelCombo.SelectedIndex = settings.LogLevel switch
        {
            LogLevel.Debug => 0,
            LogLevel.Info => 1,
            LogLevel.Warning => 2,
            LogLevel.Error => 3,
            LogLevel.None => 4,
            _ => 2,
        };
        LocalPortBox.Text = settings.LocalPort.ToString();

        LanguageCombo.SelectedIndex = settings.Language switch
        {
            AppLanguage.Fa => 0,
            AppLanguage.En => 1,
            _ => 0,
        };
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(LocalPortBox.Text, out var port) || port <= 0 || port > 65535)
        {
            System.Windows.MessageBox.Show("پورت نامعتبر است (1-65535)", "خطا",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        SelectedMode = ModeCombo.SelectedIndex switch
        {
            0 => ConnectionMode.SystemProxy,
            1 => ConnectionMode.Tun,
            2 => ConnectionMode.Direct,
            _ => ConnectionMode.SystemProxy,
        };
        AutoConnect = AutoConnectCheck.IsChecked == true;
        LocalPort = port;

        SelectedLogLevel = LogLevelCombo.SelectedIndex switch
        {
            0 => LogLevel.Debug,
            1 => LogLevel.Info,
            2 => LogLevel.Warning,
            3 => LogLevel.Error,
            4 => LogLevel.None,
            _ => LogLevel.Warning,
        };

        SelectedLanguage = LanguageCombo.SelectedIndex switch
        {
            0 => AppLanguage.Fa,
            1 => AppLanguage.En,
            _ => AppLanguage.Fa,
        };

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
