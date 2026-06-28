using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using AIXray.Core;

namespace AIXray.Network;

public interface IServerTester
{
    Task<int?> TestLatencyAsync(Server server, CancellationToken ct = default);
    Task<List<ServerTestResult>> TestAllAsync(IEnumerable<Server> servers, CancellationToken ct = default);
}

public record ServerTestResult(long ServerId, string Remark, int? LatencyMs, bool Success, string? Error);

public class ServerTester : IServerTester
{
    public async Task<int?> TestLatencyAsync(Server server, CancellationToken ct = default)
    {
        var socksPort = GetAvailablePort();
        var httpPort = socksPort + 1;

        var configDir = Path.Combine(Path.GetTempPath(), "aixray-test-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(configDir);
            var configPath = Path.Combine(configDir, "config.json");
            var tempConfig = BuildTempConfig(server, socksPort, httpPort);
            var json = tempConfig.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(configPath, json);

            var xrayPath = GetXrayPath();
            if (!File.Exists(xrayPath)) return null;

            using var testProcess = new System.Diagnostics.Process();
            testProcess.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = xrayPath,
                Arguments = $"run -c \"{configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(xrayPath) ?? "",
            };
            testProcess.Start();

            await Task.Delay(1500, ct);

            var latency = await MeasureHttpLatencyAsync($"socks5://127.0.0.1:{socksPort}", ct);

            try
            {
                testProcess.Kill(entireProcessTree: true);
                await testProcess.WaitForExitAsync(ct);
            }
            catch { }

            return latency;
        }
        catch
        {
            return null;
        }
        finally
        {
            try { Directory.Delete(configDir, true); } catch { }
        }
    }

    public async Task<List<ServerTestResult>> TestAllAsync(
        IEnumerable<Server> servers, CancellationToken ct = default)
    {
        var results = new List<ServerTestResult>();
        foreach (var server in servers)
        {
            ct.ThrowIfCancellationRequested();
            var latency = await TestLatencyAsync(server, ct);
            results.Add(new ServerTestResult(
                server.Id, server.Remark, latency, latency.HasValue, latency.HasValue ? null : "timeout"));
        }
        return results;
    }

    private static async Task<int?> MeasureHttpLatencyAsync(string proxyUrl, CancellationToken ct)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxyUrl),
                UseProxy = true,
            };

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var response = await client.GetAsync("http://www.gstatic.com/generate_204", ct);
            sw.Stop();

            return response.IsSuccessStatusCode ? (int)sw.ElapsedMilliseconds : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ساخت کانفیگ موقت با outbound واقعی سرور برای تست_latency.
    /// </summary>
    private static JsonObject BuildTempConfig(Server server, int socksPort, int httpPort)
    {
        var proxyOutbound = BuildServerOutbound(server);

        return new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "none" },
            ["inbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["tag"] = "socks-in",
                    ["protocol"] = "socks",
                    ["listen"] = "127.0.0.1",
                    ["port"] = socksPort,
                    ["settings"] = new JsonObject
                    {
                        ["auth"] = "noauth",
                        ["udp"] = true,
                    },
                },
                new JsonObject
                {
                    ["tag"] = "http-in",
                    ["protocol"] = "http",
                    ["listen"] = "127.0.0.1",
                    ["port"] = httpPort,
                },
            },
            ["outbounds"] = new JsonArray
            {
                proxyOutbound,
                new JsonObject
                {
                    ["tag"] = "direct",
                    ["protocol"] = "freedom",
                },
            },
            ["routing"] = new JsonObject
            {
                ["domainStrategy"] = "IPIfNonMatch",
                ["rules"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "field",
                        ["ip"] = new JsonArray { "geoip:private" },
                        ["outboundTag"] = "direct",
                    },
                },
            },
        };
    }

    /// <summary>
    /// ساخت outbound JSON از مدل Server (مشابه XrayConfigBuilder.BuildServerOutbound).
    /// </summary>
    private static JsonObject BuildServerOutbound(Server server)
    {
        var outbound = new JsonObject
        {
            ["tag"] = "proxy",
            ["protocol"] = server.Protocol.ToXrayName(),
            ["settings"] = BuildProtocolSettings(server),
            ["streamSettings"] = BuildStreamSettings(server),
        };
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

        switch (server.Network)
        {
            case NetworkType.WebSocket:
                var ws = new JsonObject();
                if (!string.IsNullOrEmpty(server.WsPath))
                    ws["path"] = server.WsPath;
                if (!string.IsNullOrEmpty(server.WsHost))
                    ws["headers"] = new JsonObject { ["Host"] = server.WsHost };
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
                stream["xhttpSettings"] = xh;
                break;
        }

        return stream;
    }

    private static string GetXrayPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "AIXray", "runtime", "xray.exe");
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
