using System.Web;
using AIXray.Core;

namespace AIXray.ShareLinks;

/// <summary>
/// پارس‌کننده‌ی لینک‌های ss:// (Shadowsocks)
/// فرمت‌ها:
///   ss://base64(method:password)@host:port#remark         — SIP002
///   ss://base64(method:password@host:port)#remark             — SIP002 alternative
/// </summary>
public class ShadowsocksParser : IShareLinkParser
{
    public Protocol Protocol => Protocol.Shadowsocks;
    public string Scheme => "ss://";

    public Server? TryParse(string link)
    {
        try
        {
            if (!link.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
                return null;

            var hashIdx = link.IndexOf('#');
            string remark = "";
            var mainPart = link;

            if (hashIdx >= 0)
            {
                remark = HttpUtility.UrlDecode(link.Substring(hashIdx + 1));
                mainPart = link.Substring(0, hashIdx);
            }

            mainPart = mainPart.Substring(Scheme.Length);

            // فرمت SIP002: ss://base64(method:password)@host:port
            var atIdx = mainPart.LastIndexOf('@');
            if (atIdx >= 0)
            {
                var encoded = mainPart.Substring(0, atIdx);
                var hostPort = mainPart.Substring(atIdx + 1);

                var decoded = DecodeBase64(encoded);

                // decoded باید "method:password" باشد
                var colonIdx = decoded.IndexOf(':');
                if (colonIdx < 0) return null;

                var method = decoded.Substring(0, colonIdx);
                var password = decoded.Substring(colonIdx + 1);

                if (!ParseHostPort(hostPort, out var address, out var port))
                    return null;

                var server = new Server
                {
                    Protocol = Protocol.Shadowsocks,
                    Remark = string.IsNullOrWhiteSpace(remark) ? $"{address}:{port}" : remark,
                    Address = address,
                    Port = port,
                    Method = method,
                    Password = password,
                    Network = NetworkType.Raw,
                    Security = SecurityType.None,
                };

                server.Url = link;
                return server;
            }

            // فرمت قدیمی: ss://base64(method:password@host:port)
            var decodedAll = DecodeBase64(mainPart);
            var fullColonIdx = decodedAll.IndexOf(':');
            if (fullColonIdx < 0) return null;

            var fullMethod = decodedAll.Substring(0, fullColonIdx);
            var rest = decodedAll.Substring(fullColonIdx + 1);

            var restAtIdx = rest.LastIndexOf('@');
            if (restAtIdx < 0) return null;

            var fullPassword = rest.Substring(0, restAtIdx);
            var fullHostPort = rest.Substring(restAtIdx + 1);

            if (!ParseHostPort(fullHostPort, out var fullAddress, out var fullPort))
                return null;

            var server2 = new Server
            {
                Protocol = Protocol.Shadowsocks,
                Remark = string.IsNullOrWhiteSpace(remark) ? $"{fullAddress}:{fullPort}" : remark,
                Address = fullAddress,
                Port = fullPort,
                Method = fullMethod,
                Password = fullPassword,
                Network = NetworkType.Raw,
                Security = SecurityType.None,
            };

            server2.Url = link;
            return server2;
        }
        catch
        {
            return null;
        }
    }

    private static string DecodeBase64(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        var bytes = Convert.FromBase64String(input);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static bool ParseHostPort(string input, out string address, out int port)
    {
        address = "";
        port = 0;
        if (input.StartsWith('['))
        {
            var closeBracket = input.IndexOf(']');
            if (closeBracket < 0) return false;
            address = input.Substring(1, closeBracket - 1);
            var remaining = input.Substring(closeBracket + 1);
            if (remaining.StartsWith(':'))
                int.TryParse(remaining.Substring(1), out port);
            else port = 8388;
            return true;
        }

        var colonIdx = input.LastIndexOf(':');
        if (colonIdx < 0) return false;
        address = input.Substring(0, colonIdx);
        return int.TryParse(input.Substring(colonIdx + 1), out port);
    }
}
