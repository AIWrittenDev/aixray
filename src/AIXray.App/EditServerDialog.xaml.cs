using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class EditServerDialog : FluentWindow
{
    public Server? Result { get; private set; }
    private readonly long _serverId;

    public EditServerDialog(Server server)
    {
        InitializeComponent();
        _serverId = server.Id;

        RemarkBox.Text = server.Remark;
        AddressBox.Text = server.Address;
        PortBox.Text = server.Port.ToString();
        CredentialBox.Text = server.Uuid ?? server.Password ?? "";
        SniBox.Text = server.Sni ?? "";
        FlowBox.Text = server.Flow ?? "";

        ProtocolCombo.SelectedIndex = server.Protocol switch
        {
            Protocol.Vless => 0,
            Protocol.Vmess => 1,
            Protocol.Trojan => 2,
            Protocol.Shadowsocks => 3,
            _ => 0,
        };

        NetworkCombo.SelectedIndex = server.Network switch
        {
            NetworkType.Raw => 0,
            NetworkType.WebSocket => 1,
            NetworkType.Grpc => 2,
            NetworkType.HttpUpgrade => 3,
            NetworkType.Xhttp => 4,
            _ => 0,
        };

        SecurityCombo.SelectedIndex = server.Security switch
        {
            SecurityType.None => 0,
            SecurityType.Tls => 1,
            SecurityType.Reality => 2,
            _ => 0,
        };
    }

    private void OnSave(object sender, RoutedEventArgs e)
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
            Id = _serverId,
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
