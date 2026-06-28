namespace AIXray.App;

/// <summary>
/// مدیریت اجرای خودکار برنامه هنگام استارت ویندوز.
/// </summary>
public static class AutoStartManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AIXray";

    public static bool IsAutoStartEnabled
    {
        get
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void Enable()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? "";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.SetValue(AppName, $"\"{exePath}\" --minimized");
        }
        catch
        {
            // نیاز به دسترسی
        }
    }

    public static void Disable()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.DeleteValue(AppName, false);
        }
        catch
        {
            // نیاز به دسترسی
        }
    }
}
