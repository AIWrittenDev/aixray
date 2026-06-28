using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AIXray.Proxies;

/// <summary>
/// مدیریت حالت TUN — بررسی نیاز به admin، دانلود/کپی wintun.dll، و کمک به راه‌اندازی.
/// </summary>
public interface ITunManager
{
    /// <summary>آیا برنامه با دسترسی admin اجرا شده؟</summary>
    bool IsRunningAsAdmin { get; }

    /// <summary>آیا فایل wintun.dll در مسیر مورد نیاز وجود دارد؟</summary>
    bool IsWintunAvailable { get; }

    /// <summary>مسیر wintun.dll.</summary>
    string WintunPath { get; }

    /// <summary>تلاش برای اجرای مجدد برنامه با دسترسی admin.</summary>
    bool RestartAsAdmin();
}

public class TunManager : ITunManager
{
    private readonly string _runtimeDir;

    public TunManager()
    {
        _runtimeDir = Path.Combine(AppContext.BaseDirectory, "cores");
    }

    public bool IsRunningAsAdmin
    {
        get
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public string WintunPath => Path.Combine(_runtimeDir, "wintun.dll");
    public bool IsWintunAvailable => File.Exists(WintunPath);

    public bool RestartAsAdmin()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? "";
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? "",
            };
            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
