using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class CustomConfigDialog : FluentWindow
{
    public Server? Result { get; private set; }

    public CustomConfigDialog()
    {
        InitializeComponent();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port <= 0)
        {
            System.Windows.MessageBox.Show("پورت نامعتبر است", "خطا",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var protocol = ProtocolCombo.SelectedIndex switch
        {
            0 => Protocol.Vless,
            1 => Protocol.Vmess,
            2 => Protocol.Trojan,
            3 => Protocol.Shadowsocks,
            _ => Protocol.Vless,
        };

        var network = NetworkCombo.SelectedIndex switch
        {
            0 => NetworkType.Raw,
            1 => NetworkType.WebSocket,
            2 => NetworkType.Grpc,
            3 => NetworkType.HttpUpgrade,
            4 => NetworkType.Xhttp,
            _ => NetworkType.Raw,
        };

        var security = SecurityCombo.SelectedIndex switch
        {
            0 => SecurityType.None,
            1 => SecurityType.Tls,
            2 => SecurityType.Reality,
            _ => SecurityType.None,
        };

        var address = AddressBox.Text.Trim();
        var remark = string.IsNullOrWhiteSpace(RemarkBox.Text)
            ? $"{address}:{port}"
            : RemarkBox.Text.Trim();

        Result = new Server
        {
            Remark = remark,
            Protocol = protocol,
            Address = address,
            Port = port,
            Uuid = CredentialBox.Text.Trim(),
            Password = CredentialBox.Text.Trim(),
            Network = network,
            Security = security,
            Sni = SniBox.Text.Trim(),
            Flow = FlowBox.Text.Trim(),
            Url = $"{protocol.ToXrayName()}://{CredentialBox.Text.Trim()}@{address}:{port}#{remark}",
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
