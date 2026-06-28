using System.Net;
using System.Net.Sockets;
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
            var tempConfig = BuildTempConfig(socksPort, httpPort);
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

    private static System.Text.Json.Nodes.JsonObject BuildTempConfig(int socksPort, int httpPort)
    {
        return new System.Text.Json.Nodes.JsonObject
        {
            ["log"] = new System.Text.Json.Nodes.JsonObject { ["loglevel"] = "none" },
            ["inbounds"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject
                {
                    ["tag"] = "socks-in",
                    ["protocol"] = "socks",
                    ["listen"] = "127.0.0.1",
                    ["port"] = socksPort,
                    ["settings"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["auth"] = "noauth",
                        ["udp"] = true,
                    },
                },
                new System.Text.Json.Nodes.JsonObject
                {
                    ["tag"] = "http-in",
                    ["protocol"] = "http",
                    ["listen"] = "127.0.0.1",
                    ["port"] = httpPort,
                },
            },
            ["outbounds"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject
                {
                    ["tag"] = "proxy",
                    ["protocol"] = "freedom",
                    ["settings"] = new System.Text.Json.Nodes.JsonObject(),
                },
            },
        };
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
