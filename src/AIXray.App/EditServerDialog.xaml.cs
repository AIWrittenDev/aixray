using System.Windows;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class EditServerDialog : FluentWindow
{
    public Server? Result { get; private set; }
    private readonly Server _original;

    public EditServerDialog(Server server)
    {
        InitializeComponent();
        _original = server;

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
        if (string.IsNullOrWhiteSpace(AddressBox.Text))
        {
            System.Windows.MessageBox.Show("آدرس سرور را وارد کنید", "خطا",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(PortBox.Text, out var port) || port <= 0 || port > 65535)
        {
            System.Windows.MessageBox.Show("پورت نامعتبر است (1-65535)", "خطا",
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

        var credential = CredentialBox.Text.Trim();

        Result = new Server
        {
            Id = _original.Id,
            GroupId = _original.GroupId,
            Remark = remark,
            Protocol = protocol,
            Address = address,
            Port = port,
            Uuid = protocol == Protocol.Vless || protocol == Protocol.Vmess ? credential : _original.Uuid,
            Encryption = _original.Encryption,
            Password = protocol == Protocol.Trojan || protocol == Protocol.Shadowsocks ? credential : _original.Password,
            Method = _original.Method,
            Flow = FlowBox.Text.Trim(),
            AlterId = _original.AlterId,
            Network = network,
            Security = security,
            Sni = SniBox.Text.Trim(),
            Fingerprint = _original.Fingerprint,
            Alpn = _original.Alpn,
            PublicKey = _original.PublicKey,
            ShortId = _original.ShortId,
            SpiderX = _original.SpiderX,
            WsPath = _original.WsPath,
            WsHost = _original.WsHost,
            GrpcServiceName = _original.GrpcServiceName,
            GrpcMultiMode = _original.GrpcMultiMode,
            HttpHost = _original.HttpHost,
            HttpPath = _original.HttpPath,
            XhttpExtra = _original.XhttpExtra,
            Url = _original.Url,
            LatencyMs = _original.LatencyMs,
            IsActive = _original.IsActive,
            LastTest = _original.LastTest,
            AddedAt = _original.AddedAt,
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
