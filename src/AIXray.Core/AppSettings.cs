namespace AIXray.Core;

/// <summary>
/// تنظیمات سراسری برنامه. به‌صورت یک ردیف واحد در جدول settings ذخیره می‌شود.
/// </summary>
public class AppSettings
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>پورت socks لوکال (http روی پورت +1 گوش می‌دهد).</summary>
    public int LocalPort { get; set; } = 10808;

    /// <summary>آیا inboundها روی همه‌ی رابط‌ها (0.0.0.0) گوش دهند تا در شبکه‌ی لوکال به اشتراک گذاشته شوند؟</summary>
    public bool ShareLocal { get; set; } = false;

    /// <summary>حالت اتصال فعلی.</summary>
    public ConnectionMode Mode { get; set; } = ConnectionMode.SystemProxy;

    /// <summary>زبان رابط کاربری.</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Fa;

    /// <summary>مسیر فایل اجرایی xray.exe (در صورت null، از مسیر پیش‌فرض استفاده می‌شود).</summary>
    public string? XrayBinaryPath { get; set; }

    /// <summary>آیا هنگام اجرای برنامه به‌صورت خودکار متصل شود؟</summary>
    public bool AutoConnect { get; set; } = false;
}
