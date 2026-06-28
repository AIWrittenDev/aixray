using System.Text.Json;
using AIXray.Core;

namespace AIXray.ShareLinks;

/// <summary>
/// پارس‌کننده‌ی لینک‌های vmess://
/// فرمت: vmess://base64(JSON) — استاندارد v2rayN
/// </summary>
public class VmessParser : IShareLinkParser
{
    public Protocol Protocol => Protocol.Vmess;
    public string Scheme => "vmess://";

    public Server? TryParse(string link)
    {
        try
        {
            if (!link.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
                return null;

            var base64Part = link.Substring(Scheme.Length).Trim();
            var json = DecodeBase64(base64Part);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var address = TryGetString(root, "add") ?? "";
            var portStr = TryGetString(root, "port") ?? "0";
            var uuid = TryGetString(root, "id") ?? "";
            var aidStr = TryGetString(root, "aid") ?? "0";

            if (!int.TryParse(portStr, out var port) || port <= 0)
                return null;

            var net = TryGetString(root, "net") ?? "tcp";
            var security = TryGetString(root, "tls") ?? "";
            var remark = TryGetString(root, "ps") ?? $"{address}:{port}";

            var server = new Server
            {
                Protocol = Protocol.Vmess,
                Remark = remark,
                Address = address,
                Port = port,
                Uuid = uuid,
                AlterId = int.TryParse(aidStr, out var aid) ? aid : 0,
                Encryption = "auto",
                Network = EnumMappings.NetworkFromName(net),
                Security = EnumMappings.SecurityFromName(security),
                Sni = TryGetString(root, "sni"),
                Fingerprint = TryGetString(root, "fp"),
                Alpn = TryGetString(root, "alpn"),
                PublicKey = TryGetString(root, "pbk"),
                ShortId = TryGetString(root, "sid"),
                WsPath = TryGetString(root, "path"),
                WsHost = TryGetString(root, "host"),
                GrpcServiceName = TryGetString(root, "path"),
                HttpPath = TryGetString(root, "path"),
                HttpHost = TryGetString(root, "host"),
            };

            server.Url = link;
            return server;
        }
        catch
        {
            return null;
        }
    }

    private static string DecodeBase64(string input)
    {
        // base64url یا base64 استاندارد
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        var bytes = Convert.FromBase64String(input);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var prop)
            ? prop.ValueKind == JsonValueKind.String ? prop.GetString() : null
            : null;
    }
}
