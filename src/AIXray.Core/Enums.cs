namespace AIXray.Core;

/// <summary>
/// پروتکل‌های خروجی (outbound) پشتیبانی‌شده در xray.
/// </summary>
public enum Protocol
{
    Vless,
    Vmess,
    Trojan,
    Shadowsocks,
}

/// <summary>
/// نوع لایه‌ی انتقال (network / transport).
/// نکته: xray در نسخه‌های جدید برای TCP از نام "raw" استفاده می‌کند.
/// </summary>
public enum NetworkType
{
    Raw,        // tcp (نام مدرن در xray: raw)
    WebSocket,  // websocket
    Grpc,       // grpc
    Kcp,        // mkcp
    HttpUpgrade,// httpupgrade
    Xhttp,      // xhttp
}

/// <summary>
/// نوع امنیت انتقال.
/// </summary>
public enum SecurityType
{
    None,
    Tls,
    Reality,
}

/// <summary>
/// سطح لاگ xray.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    None,
}

/// <summary>
/// حالت اتصال/عملکرد برنامه.
/// </summary>
public enum ConnectionMode
{
    /// <summary>بدون پروکسی — xray فقط روی لوکال گوش می‌دهد.</summary>
    Direct,

    /// <summary>اعمال پروکسی سیستم (تغییر تنظیمات پروکسی ویندوز).</summary>
    SystemProxy,

    /// <summary>حالت VPN با استفاده از TUN (نیازمند admin و wintun).</summary>
    Tun,
}

/// <summary>
/// زبان رابط کاربری.
/// </summary>
public enum AppLanguage
{
    Fa,
    En,
}
