using System.Runtime.CompilerServices;

namespace AIXray.Core;

/// <summary>
/// متدهای کمکی برای تبدیل مقادیر enum به/از نام‌های مورد انتظار xray در کانفیگ JSON.
/// همه‌ی تبدیل‌ها در یک مکان متمرکز شده‌اند تا با تغییر نسخه‌ی xray، فقط اینجا ویرایش شود.
/// </summary>
public static class EnumMappings
{
    // ----- Protocol → نام xray -----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToXrayName(this Protocol p) => p switch
    {
        Protocol.Vless => "vless",
        Protocol.Vmess => "vmess",
        Protocol.Trojan => "trojan",
        Protocol.Shadowsocks => "shadowsocks",
        _ => p.ToString().ToLowerInvariant(),
    };

    public static Protocol ProtocolFromName(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "vless" => Protocol.Vless,
        "vmess" => Protocol.Vmess,
        "trojan" => Protocol.Trojan,
        "shadowsocks" or "ss" => Protocol.Shadowsocks,
        _ => Protocol.Vless,
    };

    // ----- NetworkType → نام xray -----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToXrayName(this NetworkType n) => n switch
    {
        NetworkType.Raw => "raw",
        NetworkType.WebSocket => "websocket",
        NetworkType.Grpc => "grpc",
        NetworkType.Kcp => "mkcp",
        NetworkType.HttpUpgrade => "httpupgrade",
        NetworkType.Xhttp => "xhttp",
        _ => n.ToString().ToLowerInvariant(),
    };

    public static NetworkType NetworkFromName(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "raw" or "tcp" => NetworkType.Raw,
        "ws" or "websocket" => NetworkType.WebSocket,
        "grpc" => NetworkType.Grpc,
        "kcp" or "mkcp" => NetworkType.Kcp,
        "httpupgrade" => NetworkType.HttpUpgrade,
        "xhttp" => NetworkType.Xhttp,
        _ => NetworkType.Raw,
    };

    // ----- SecurityType → نام xray -----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToXrayName(this SecurityType s) => s switch
    {
        SecurityType.None => "none",
        SecurityType.Tls => "tls",
        SecurityType.Reality => "reality",
        _ => s.ToString().ToLowerInvariant(),
    };

    public static SecurityType SecurityFromName(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "" or "none" => SecurityType.None,
        "tls" => SecurityType.Tls,
        "reality" => SecurityType.Reality,
        _ => SecurityType.None,
    };

    // ----- LogLevel → نام xray -----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToXrayName(this LogLevel l) => l switch
    {
        LogLevel.Debug => "debug",
        LogLevel.Info => "info",
        LogLevel.Warning => "warning",
        LogLevel.Error => "error",
        LogLevel.None => "none",
        _ => "warning",
    };

    public static LogLevel LogLevelFromName(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "debug" => LogLevel.Debug,
        "info" => LogLevel.Info,
        "warning" => LogLevel.Warning,
        "error" => LogLevel.Error,
        "none" => LogLevel.None,
        _ => LogLevel.Warning,
    };
}
