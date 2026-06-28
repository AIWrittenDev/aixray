using System.Web;
using AIXray.Core;

namespace AIXray.ShareLinks;

/// <summary>
/// پارس‌کننده‌ی لینک‌های vless://
/// فرمت: vless://uuid@host:port?params#remark
/// </summary>
public class VlessParser : IShareLinkParser
{
    public Protocol Protocol => Protocol.Vless;
    public string Scheme => "vless://";

    public Server? TryParse(string link)
    {
        try
        {
            if (!link.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
                return null;

            // جدا کردن fragment (remark) از بقیه
            var hashIdx = link.IndexOf('#');
            string remark = "";
            var mainPart = link;

            if (hashIdx >= 0)
            {
                remark = HttpUtility.UrlDecode(link.Substring(hashIdx + 1));
                mainPart = link.Substring(0, hashIdx);
            }

            // حذف پیشوند
            mainPart = mainPart.Substring(Scheme.Length);

            // جدا کردن query string
            var queryIdx = mainPart.IndexOf('?');
            string query = "";
            if (queryIdx >= 0)
            {
                query = mainPart.Substring(queryIdx + 1);
                mainPart = mainPart.Substring(0, queryIdx);
            }

            // پارس uuid@host:port
            var atIdx = mainPart.IndexOf('@');
            if (atIdx < 0) return null;
            var uuid = mainPart.Substring(0, atIdx);
            var hostPort = mainPart.Substring(atIdx + 1);

            if (!ParseHostPort(hostPort, out var address, out var port))
                return null;

            // پارس query parameters
            var queryParams = ParseQueryString(query);

            var server = new Server
            {
                Protocol = Protocol.Vless,
                Remark = string.IsNullOrWhiteSpace(remark) ? $"{address}:{port}" : remark,
                Address = address,
                Port = port,
                Uuid = uuid,
                Encryption = queryParams.GetValueOrDefault("encryption"),
                Flow = queryParams.GetValueOrDefault("flow"),
                Network = EnumMappings.NetworkFromName(queryParams.GetValueOrDefault("type")),
                Security = EnumMappings.SecurityFromName(queryParams.GetValueOrDefault("security")),
                Sni = queryParams.GetValueOrDefault("sni"),
                Fingerprint = queryParams.GetValueOrDefault("fp"),
                Alpn = queryParams.GetValueOrDefault("alpn"),
                PublicKey = queryParams.GetValueOrDefault("pbk"),
                ShortId = queryParams.GetValueOrDefault("sid"),
                SpiderX = queryParams.GetValueOrDefault("spx"),
                WsPath = queryParams.GetValueOrDefault("path"),
                WsHost = queryParams.GetValueOrDefault("host"),
                GrpcServiceName = queryParams.GetValueOrDefault("serviceName"),
                HttpPath = queryParams.GetValueOrDefault("path"),
                HttpHost = queryParams.GetValueOrDefault("host"),
            };

            server.Url = link;
            return server;
        }
        catch
        {
            return null;
        }
    }

    private static bool ParseHostPort(string input, out string address, out int port)
    {
        address = "";
        port = 0;

        // برای IPv6: [::1]:443
        if (input.StartsWith('['))
        {
            var closeBracket = input.IndexOf(']');
            if (closeBracket < 0) return false;
            address = input.Substring(1, closeBracket - 1);
            var remaining = input.Substring(closeBracket + 1);
            if (remaining.StartsWith(':'))
            {
                if (!int.TryParse(remaining.Substring(1), out port)) return false;
            }
            else
            {
                port = 443; // پیش‌فرض
            }
            return true;
        }

        // برای IPv4 / domain
        var colonIdx = input.LastIndexOf(':');
        if (colonIdx < 0) return false;

        address = input.Substring(0, colonIdx);
        return int.TryParse(input.Substring(colonIdx + 1), out port);
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return dict;

        foreach (var pair in query.Split('&'))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx >= 0)
            {
                var key = HttpUtility.UrlDecode(pair.Substring(0, eqIdx));
                var value = HttpUtility.UrlDecode(pair.Substring(eqIdx + 1));
                dict[key] = value;
            }
            else
            {
                dict[pair] = "";
            }
        }
        return dict;
    }
}
