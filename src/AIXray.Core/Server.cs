using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIXray.Core;

/// <summary>
/// یک سرور/کانفیگ پروکسی که می‌تواند در xray استفاده شود.
/// </summary>
public class Server : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public long Id { get; set; }

    /// <summary>شناسه‌ی گروه (null = بدون گروه / همه‌ی کانفیگ‌ها).</summary>
    public long? GroupId { get; set; }

    private string _remark = string.Empty;
    /// <summary>نام نمایشی سرور.</summary>
    public string Remark
    {
        get => _remark;
        set { _remark = value; OnPropertyChanged(); }
    }

    // ----- اطلاعات اتصال پایه -----
    private Protocol _protocol;
    public Protocol Protocol
    {
        get => _protocol;
        set { _protocol = value; OnPropertyChanged(); }
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }

    private int _port;
    public int Port
    {
        get => _port;
        set { _port = value; OnPropertyChanged(); }
    }

    // ----- فیلدهای پروتکل‌اختصاصی -----
    public string? Uuid { get; set; }
    public string? Encryption { get; set; }
    public string? Password { get; set; }
    public string? Method { get; set; }
    public string? Flow { get; set; }
    public int AlterId { get; set; }

    // ----- لایه‌ی انتقال (streamSettings) -----
    private NetworkType _network = NetworkType.Raw;
    public NetworkType Network
    {
        get => _network;
        set { _network = value; OnPropertyChanged(); }
    }

    private SecurityType _security = SecurityType.None;
    public SecurityType Security
    {
        get => _security;
        set { _security = value; OnPropertyChanged(); }
    }

    public string? Sni { get; set; }
    public string? Fingerprint { get; set; }
    public string? Alpn { get; set; }
    public string? PublicKey { get; set; }
    public string? ShortId { get; set; }
    public string? SpiderX { get; set; }
    public string? WsPath { get; set; }
    public string? WsHost { get; set; }
    public string? GrpcServiceName { get; set; }
    public bool GrpcMultiMode { get; set; }
    public string? HttpHost { get; set; }
    public string? HttpPath { get; set; }
    public string? XhttpExtra { get; set; }

    // ----- وضعیت و لینک اصلی -----
    public string Url { get; set; } = string.Empty;

    private int? _latencyMs;
    /// <summary>تأخیر اندازه‌گیری‌شده بر حسب میلی‌ثانیه.</summary>
    public int? LatencyMs
    {
        get => _latencyMs;
        set { _latencyMs = value; OnPropertyChanged(); }
    }

    private bool _isActive;
    /// <summary>آیا این سرور هم‌اکنون انتخاب/فعال است؟</summary>
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public DateTime? LastTest { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>خلاصه‌ی کوتاه برای نمایش (آدرس:پورت).</summary>
    public string AddressSummary => $"{Address}:{Port}";
}
