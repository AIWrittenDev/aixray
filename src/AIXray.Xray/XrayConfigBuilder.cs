using System.Text.Json;
using System.Text.Json.Nodes;
using AIXray.Core;

namespace AIXray.Xray;

/// <summary>
/// ساخت JSON کانفیگ داینامیک برای xray-core بر اساس سرور فعال و تنظیمات برنامه.
/// خروجی: JSON object آماده برای نوشتن در فایل و ارسال به xray.exe -c.
/// </summary>
public interface IXrayConfigBuilder
{
    /// <summary>ساخت کانفیگ کامل (log + inbounds + outbounds + routing).</summary>
    JsonObject BuildConfig(Server activeServer, AppSettings settings, string configDir);
}

public class XrayConfigBuilder : IXrayConfigBuilder
{
    public JsonObject BuildConfig(Server activeServer, AppSettings settings, string configDir)
    {
        var config = new JsonObject
        {
            ["log"] = BuildLog(settings),
            ["inbounds"] = BuildInbounds(settings, configDir),
            ["outbounds"] = BuildOutbounds(activeServer),
            ["routing"] = BuildRouting(settings),
        };
        return config;
    }

    private static JsonObject BuildLog(AppSettings settings)
    {
        return new JsonObject
        {
            ["loglevel"] = settings.LogLevel.ToXrayName(),
            ["access"] = "",   // access log خاموش
            ["error"] = "",    // error log خاموش
        };
    }

    private static JsonArray BuildInbounds(AppSettings settings, string configDir)
    {
        var listen = settings.ShareLocal ? "0.0.0.0" : "127.0.0.1";
        var inbounds = new JsonArray();

        // SOCKS inbound
        inbounds.Add(new JsonObject
        {
            ["tag"] = "socks-in",
            ["protocol"] = "socks",
            ["listen"] = listen,
            ["port"] = settings.LocalPort,
            ["settings"] = new JsonObject
            {
                ["auth"] = "noauth",
                ["udp"] = true,
            },
            ["sniffing"] = new JsonObject
            {
                ["enabled"] = true,
                ["destOverride"] = new JsonArray { "http", "tls" },
                ["routeOnly"] = false,
            },
        });

        // HTTP inbound
        inbounds.Add(new JsonObject
        {
            ["tag"] = "http-in",
            ["protocol"] = "http",
            ["listen"] = listen,
            ["port"] = settings.LocalPort + 1,
            ["settings"] = new JsonObject
            {
                ["allowTransparent"] = false,
            },
        });

        // حالت TUN: inbound تون (فقط اگر Mode == Tun)
        if (settings.Mode == ConnectionMode.Tun)
        {
            inbounds.Add(BuildTunInbound(configDir));
        }

        return inbounds;
    }

    private static JsonObject BuildTunInbound(string configDir)
    {
        return new JsonObject
        {
            ["tag"] = "tun",
            ["protocol"] = "tun",
            ["settings"] = new JsonObject
            {
                ["name"] = "xray_tun",
                ["MTU"] = 1500,
                ["gateway"] = new JsonArray { "172.18.0.1/30", "fdfe:dcba:9876::1/126" },
            },
            ["sniffing"] = new JsonObject
            {
                ["enabled"] = true,
                ["destOverride"] = new JsonArray { "http", "tls" },
            },
        };
    }

    private static JsonArray BuildOutbounds(Server activeServer)
    {
        var outbounds = new JsonArray();

        // outbound اصلی — سرور فعال
        var proxyOutbound = BuildServerOutbound(activeServer);
        proxyOutbound["tag"] = "proxy";
        outbounds.Add(proxyOutbound);

        // direct
        outbounds.Add(new JsonObject
        {
            ["tag"] = "direct",
            ["protocol"] = "freedom",
            ["settings"] = new JsonObject { ["domainStrategy"] = "UseIP" },
        });

        // block
        outbounds.Add(new JsonObject
        {
            ["tag"] = "block",
            ["protocol"] = "blackhole",
        });

        return outbounds;
    }

    /// <summary>ساخت outbound JSON از مدل Server.</summary>
    public static JsonObject BuildServerOutbound(Server server)
    {
        var outbound = new JsonObject
        {
            ["protocol"] = server.Protocol.ToXrayName(),
            ["settings"] = BuildProtocolSettings(server),
            ["streamSettings"] = BuildStreamSettings(server),
        };

        // mux فقط برای VLESS با flow
        if (server.Protocol == Protocol.Vless && server.Flow != null)
        {
            outbound["mux"] = new JsonObject
            {
                ["enabled"] = false,
            };
        }

        return outbound;
    }

    private static JsonObject BuildProtocolSettings(Server server)
    {
        var settings = new JsonObject();

        switch (server.Protocol)
        {
            case Protocol.Vless:
                settings["address"] = server.Address;
                settings["port"] = server.Port;
                settings["encryption"] = server.Encryption ?? "none";
                if (!string.IsNullOrEmpty(server.Uuid))
                    settings["id"] = server.Uuid;
                if (!string.IsNullOrEmpty(server.Flow))
                    settings["flow"] = server.Flow;
                break;

            case Protocol.Vmess:
                settings["address"] = server.Address;
                settings["port"] = server.Port;
                if (!string.IsNullOrEmpty(server.Uuid))
                    settings["id"] = server.Uuid;
                settings["alterId"] = server.AlterId;
                settings["security"] = "auto";
                break;

            case Protocol.Trojan:
                settings["address"] = server.Address;
                settings["port"] = server.Port;
                if (!string.IsNullOrEmpty(server.Password))
                    settings["password"] = server.Password;
                break;

            case Protocol.Shadowsocks:
                settings["address"] = server.Address;
                settings["port"] = server.Port;
                if (!string.IsNullOrEmpty(server.Method))
                    settings["method"] = server.Method;
                if (!string.IsNullOrEmpty(server.Password))
                    settings["password"] = server.Password;
                break;
        }

        return settings;
    }

    private static JsonObject BuildStreamSettings(Server server)
    {
        var stream = new JsonObject
        {
            ["network"] = server.Network.ToXrayName(),
            ["security"] = server.Security.ToXrayName(),
        };

        // تنظیمات امنیتی
        switch (server.Security)
        {
            case SecurityType.Tls:
                var tls = new JsonObject();
                if (!string.IsNullOrEmpty(server.Sni))
                    tls["serverName"] = server.Sni;
                if (!string.IsNullOrEmpty(server.Fingerprint))
                    tls["fingerprint"] = server.Fingerprint;
                if (!string.IsNullOrEmpty(server.Alpn))
                {
                    var alpnArray = new JsonArray();
                    foreach (var alpn in server.Alpn.Split(','))
                        alpnArray.Add(alpn.Trim());
                    tls["alpn"] = alpnArray;
                }
                stream["tlsSettings"] = tls;
                break;

            case SecurityType.Reality:
                var reality = new JsonObject();
                if (!string.IsNullOrEmpty(server.Sni))
                    reality["serverName"] = server.Sni;
                if (!string.IsNullOrEmpty(server.Fingerprint))
                    reality["fingerprint"] = server.Fingerprint;
                if (!string.IsNullOrEmpty(server.PublicKey))
                    reality["publicKey"] = server.PublicKey;
                if (!string.IsNullOrEmpty(server.ShortId))
                    reality["shortId"] = server.ShortId;
                if (!string.IsNullOrEmpty(server.SpiderX))
                    reality["spiderX"] = server.SpiderX;
                stream["realitySettings"] = reality;
                break;
        }

        // تنظیمات انتقال
        switch (server.Network)
        {
            case NetworkType.WebSocket:
                var ws = new JsonObject();
                if (!string.IsNullOrEmpty(server.WsPath))
                    ws["path"] = server.WsPath;
                if (!string.IsNullOrEmpty(server.WsHost))
                {
                    ws["headers"] = new JsonObject
                    {
                        ["Host"] = server.WsHost,
                    };
                }
                stream["wsSettings"] = ws;
                break;

            case NetworkType.Grpc:
                var grpc = new JsonObject();
                if (!string.IsNullOrEmpty(server.GrpcServiceName))
                    grpc["serviceName"] = server.GrpcServiceName;
                grpc["multiMode"] = server.GrpcMultiMode;
                stream["grpcSettings"] = grpc;
                break;

            case NetworkType.Kcp:
                var kcp = new JsonObject();
                kcp["header"] = new JsonObject { ["type"] = "none" };
                stream["kcpSettings"] = kcp;
                break;

            case NetworkType.HttpUpgrade:
                var hup = new JsonObject();
                if (!string.IsNullOrEmpty(server.HttpPath))
                    hup["path"] = server.HttpPath;
                if (!string.IsNullOrEmpty(server.HttpHost))
                    hup["host"] = server.HttpHost;
                stream["httpupgradeSettings"] = hup;
                break;

            case NetworkType.Xhttp:
                var xh = new JsonObject();
                if (!string.IsNullOrEmpty(server.HttpPath))
                    xh["path"] = server.HttpPath;
                if (!string.IsNullOrEmpty(server.HttpHost))
                    xh["host"] = server.HttpHost;
                // اضافه کردن تنظیمات extra xhttp (xPaddingBytes, sessionKey, headers)
                if (!string.IsNullOrEmpty(server.XhttpExtra))
                {
                    try
                    {
                        var extraObj = JsonNode.Parse(server.XhttpExtra) as JsonObject;
                        if (extraObj != null)
                        {
                            foreach (var prop in extraObj)
                                xh[prop.Key] = prop.Value;
                        }
                    }
                    catch { /* extra is not valid JSON, ignore */ }
                }
                stream["xhttpSettings"] = xh;
                break;
        }

        return stream;
    }

    private static JsonObject BuildRouting(AppSettings settings)
    {
        var rules = new JsonArray
        {
            // IPهای خصوصی مستقیم
            new JsonObject
            {
                ["type"] = "field",
                ["ip"] = new JsonArray { "geoip:private" },
                ["outboundTag"] = "direct",
            },
            // DNS sniffed به direct
            new JsonObject
            {
                ["type"] = "field",
                ["protocol"] = new JsonArray { "dns" },
                ["outboundTag"] = "direct",
            },
        };

        // در حالت TUN، ترافیک xray خودش نباید لوپ شود
        if (settings.Mode == ConnectionMode.Tun)
        {
            rules.Add(new JsonObject
            {
                ["type"] = "field",
                ["processName"] = new JsonArray { "xray.exe", "AIXray.App.exe" },
                ["outboundTag"] = "direct",
            });
        }

        return new JsonObject
        {
            ["domainStrategy"] = "IPIfNonMatch",
            ["rules"] = rules,
        };
    }
}
