using AIXray.Core;
using AIXray.Proxies;
using AIXray.Storage;
using AIXray.Xray;

namespace AIXray.Network;

public interface IAutoConnectService
{
    Task<bool> ConnectToBestAsync(AppSettings settings, CancellationToken ct = default);
}

public class AutoConnectService : IAutoConnectService
{
    private readonly IServerRepository _serverRepo;
    private readonly IServerTester _tester;
    private readonly IXrayConfigBuilder _configBuilder;
    private readonly IXrayDownloader _xrayDownloader;
    private readonly IXrayProcessManager _processManager;
    private readonly ISystemProxyManager _systemProxyManager;

    public AutoConnectService(
        IServerRepository serverRepo,
        IServerTester tester,
        IXrayConfigBuilder configBuilder,
        IXrayDownloader xrayDownloader,
        IXrayProcessManager processManager,
        ISystemProxyManager systemProxyManager)
    {
        _serverRepo = serverRepo;
        _tester = tester;
        _configBuilder = configBuilder;
        _xrayDownloader = xrayDownloader;
        _processManager = processManager;
        _systemProxyManager = systemProxyManager;
    }

    public async Task<bool> ConnectToBestAsync(AppSettings settings, CancellationToken ct = default)
    {
        var servers = await _serverRepo.GetAllAsync();
        if (servers.Count == 0) return false;

        var results = await _tester.TestAllAsync(servers, ct);

        var best = results
            .Where(r => r.Success && r.LatencyMs.HasValue)
            .OrderBy(r => r.LatencyMs!.Value)
            .FirstOrDefault();

        if (best == null) return false;

        var server = servers.FirstOrDefault(s => s.Id == best.ServerId);
        if (server == null) return false;

        await _serverRepo.DeactivateAllAsync();
        await _serverRepo.SetActiveAsync(server.Id, active: true);

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIXray", "config-generated");
        var config = _configBuilder.BuildConfig(server, settings, configDir);
        await _processManager.StartAsync(_xrayDownloader.XrayExePath, config, configDir);

        // اعمال پروکسی سیستم
        if (settings.Mode == ConnectionMode.SystemProxy)
        {
            _systemProxyManager.Enable(settings.LocalPort);
        }

        return true;
    }
}
