using System.Security.Principal;
using AIXray.Core;

namespace AIXray.Proxies;

/// <summary>
/// مدیریت پروکسی سیستم ویندوز از طریق رجیستری.
/// </summary>
public interface ISystemProxyManager
{
    /// <summary>اعمال پروکسی سیستم.</summary>
    void Enable(int port);

    /// <summary>حذف پروکسی سیستم.</summary>
    void Disable();

    /// <summary>آیا پروکسی سیستم فعال است؟</summary>
    bool IsEnabled { get; }

    /// <summary>پورت فعلی.</summary>
    int? CurrentPort { get; }
}

public class SystemProxyManager : ISystemProxyManager
{
    private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    public bool IsEnabled { get; private set; }
    public int? CurrentPort { get; private set; }

    public void Enable(int port)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true);
            if (key == null) return;

            // فعال‌سازی پروکسی
            key.SetValue("ProxyEnable", 1, Microsoft.Win32.RegistryValueKind.DWord);
            key.SetValue("ProxyServer", $"127.0.0.1:{port}", Microsoft.Win32.RegistryValueKind.String);

            // پروکسی برای همه پروتکل‌ها
            key.SetValue("ProxyOverride", "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>", Microsoft.Win32.RegistryValueKind.String);

            // اطلاع‌رسانی به ویندوز
            InternetSetOption(0, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(0, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            IsEnabled = true;
            CurrentPort = port;
        }
        catch
        {
            // نیاز به دسترسی admin
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true);
            if (key == null) return;

            key.SetValue("ProxyEnable", 0, Microsoft.Win32.RegistryValueKind.DWord);

            InternetSetOption(0, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(0, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            IsEnabled = false;
            CurrentPort = null;
        }
        catch
        {
            // نیاز به دسترسی admin
        }
    }

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;

    [System.Runtime.InteropServices.DllImport("wininet.dll")]
    private static extern bool InternetSetOption(int hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
}
