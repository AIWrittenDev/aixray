using AIXray.Core;
using AIXray.Storage;
using AIXray.Xray;

namespace AIXray.Network;

/// <summary>
/// منطق اتصال خودکار — تست همه سرورها و اتصال به اولین سرور فعال.
/// </summary>
public interface IAutoConnectService
{
    /// <summary>تست همه سرورها و اتصال به بهترین.</summary>
    Task<bool> ConnectToBestAsync(AppSettings settings, CancellationToken ct = default);
}

public class AutoConnectService : IAutoConnectService
{
    private readonly IServerRepository _serverRepo;
    private readonly IServerTester _tester;
    private readonly IXrayConfigBuilder _configBuilder;
    private readonly IXrayDownloader _xrayDownloader;
    private readonly IXrayProcessManager _processManager;

    public AutoConnectService(
        IServerRepository serverRepo,
        IServerTester tester,
        IXrayConfigBuilder configBuilder,
        IXrayDownloader xrayDownloader,
        IXrayProcessManager processManager)
    {
        _serverRepo = serverRepo;
        _tester = tester;
        _configBuilder = configBuilder;
        _xrayDownloader = xrayDownloader;
        _processManager = processManager;
    }

    public async Task<bool> ConnectToBestAsync(AppSettings settings, CancellationToken ct = default)
    {
        var servers = await _serverRepo.GetAllAsync();
        if (servers.Count == 0) return false;

        // تست همه سرورها
        var results = await _tester.TestAllAsync(servers, ct);

        // مرتب‌سازی بر اساس تأخیر (کمترین اول)
        var best = results
            .Where(r => r.Success && r.LatencyMs.HasValue)
            .OrderBy(r => r.LatencyMs!.Value)
            .FirstOrDefault();

        if (best == null) return false;

        // پیدا کردن سرور مربوطه
        var server = servers.FirstOrDefault(s => s.Id == best.ServerId);
        if (server == null) return false;

        // فعال‌سازی سرور
        await _serverRepo.DeactivateAllAsync();
        await _serverRepo.SetActiveAsync(server.Id, active: true);

        // اتصال
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIXray", "config-generated");
        var config = _configBuilder.BuildConfig(server, settings, configDir);
        await _processManager.StartAsync(_xrayDownloader.XrayExePath, config, configDir);

        return true;
    }
}
