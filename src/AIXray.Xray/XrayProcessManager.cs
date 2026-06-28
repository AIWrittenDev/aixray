using System.Diagnostics;
using System.Text.Json.Nodes;
using AIXray.Core;

namespace AIXray.Xray;

/// <summary>
/// مدیریت چرخه‌ی حیات پروسه‌ی xray-core (Start / Stop / Restart).
/// لاگ stdout/stderr را capture و از طریق رویداد ارائه می‌دهد.
/// </summary>
public interface IXrayProcessManager : IDisposable
{
    /// <summary>آیا پروسه‌ی xray هم‌اکنون در حال اجراست؟</summary>
    bool IsRunning { get; }

    /// <summary>رویداد دریافت هر خط لاگ از xray.</summary>
    event EventHandler<string>? LogReceived;

    /// <summary>رویداد تغییر وضعیت (شروع / توقف).</summary>
    event EventHandler<bool>? StateChanged;

    /// <summary>شروع xray با کانفیگ مشخص.</summary>
    Task StartAsync(string xrayExePath, JsonObject config, string configDir);

    /// <summary>شروع xray با فایل کانفیگ موجود.</summary>
    Task StartAsync(string xrayExePath, string configPath);

    /// <summary>توقف xray.</summary>
    Task StopAsync();

    /// <summary>ری‌استارت (stop + start).</summary>
    Task RestartAsync(string xrayExePath, JsonObject config, string configDir);
}

public class XrayProcessManager : IXrayProcessManager
{
    private Process? _process;
    private CancellationTokenSource? _readCts;
    private readonly object _lock = new();

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _process != null && !_process.HasExited;
            }
        }
    }

    public event EventHandler<string>? LogReceived;
    public event EventHandler<bool>? StateChanged;

    public async Task StartAsync(string xrayExePath, JsonObject config, string configDir)
    {
        // نوشتن کانفیگ به فایل موقت
        Directory.CreateDirectory(configDir);
        var configPath = Path.Combine(configDir, "config.json");
        var json = config.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);

        await StartAsync(xrayExePath, configPath);
    }

    public async Task StartAsync(string xrayExePath, string configPath)
    {
        await StopAsync(); // مطمئن شویم قبلی بسته شده

        if (!File.Exists(xrayExePath))
            throw new FileNotFoundException($"xray.exe not found at: {xrayExePath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = xrayExePath,
            Arguments = $"run -c \"{configPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(xrayExePath) ?? "",
        };

        var process = new Process { StartInfo = startInfo };

        _readCts?.Cancel();
        _readCts?.Dispose();
        _readCts = new CancellationTokenSource();

        lock (_lock)
        {
            _process = process;
        }

        process.Start();
        StateChanged?.Invoke(this, true);

        // خواندن لاگ‌ها در background
        _ = ReadStreamAsync(process.StandardOutput, _readCts.Token);
        _ = ReadStreamAsync(process.StandardError, _readCts.Token);

        // وقتی پروسه‌ی xray exit شد
        _ = Task.Run(() =>
        {
            process.WaitForExit();
            lock (_lock)
            {
                if (_process == process)
                    _process = null;
            }
            StateChanged?.Invoke(this, false);
        });
    }

    public async Task StopAsync()
    {
        Process? processToKill;
        lock (_lock)
        {
            processToKill = _process;
            _process = null;
        }

        if (processToKill != null && !processToKill.HasExited)
        {
            try
            {
                processToKill.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await processToKill.WaitForExitAsync(cts.Token);
            }
            catch (InvalidOperationException)
            {
                // قبلاً بسته شده
            }
            catch (OperationCanceledException)
            {
                // timeout
            }
            finally
            {
                processToKill.Dispose();
            }
            StateChanged?.Invoke(this, false);
        }

        _readCts?.Cancel();
    }

    public async Task RestartAsync(string xrayExePath, JsonObject config, string configDir)
    {
        await StartAsync(xrayExePath, config, configDir);
    }

    private async Task ReadStreamAsync(StreamReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                LogReceived?.Invoke(this, line);
            }
        }
        catch (OperationCanceledException)
        {
            // طبیعی — cancel شده
        }
        catch (Exception)
        {
            // استریم بسته شده
        }
    }

    public void Dispose()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        try { _process?.Kill(entireProcessTree: true); } catch { }
        _process?.Dispose();
    }
}
