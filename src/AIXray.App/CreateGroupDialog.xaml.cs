using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class CreateGroupDialog : FluentWindow
{
    public string GroupName { get; private set; } = string.Empty;
    public string? SubscriptionUrl { get; private set; }
    public bool AutoUpdate { get; private set; }
    public int UpdateIntervalMinutes { get; private set; } = 60;

    public CreateGroupDialog()
    {
        InitializeComponent();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            System.Windows.MessageBox.Show("نام گروه را وارد کنید", "خطا",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        GroupName = NameBox.Text.Trim();
        SubscriptionUrl = string.IsNullOrWhiteSpace(UrlBox.Text) ? null : UrlBox.Text.Trim();
        AutoUpdate = AutoUpdateCheck.IsChecked == true;

        if (!int.TryParse(IntervalBox.Text, out var interval) || interval <= 0)
            interval = 60;
        UpdateIntervalMinutes = interval;

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
