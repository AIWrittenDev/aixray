namespace AIXray.Core;

/// <summary>
/// یک سرور/کانفیگ پروکسی که می‌تواند در xray استفاده شود.
/// این مدل خالص (POCO) است و فیلدهای لازم برای همه‌ی پروتکل‌ها را در خود دارد.
/// لینک اشتراک اصلی در <see cref="Url"/> نگهداری می‌شود تا همیشه قابل بازتولید باشد.
/// </summary>
public class Server
{
    public long Id { get; set; }

    /// <summary>شناسه‌ی گروه (null = بدون گروه / همه‌ی کانفیگ‌ها).</summary>
    public long? GroupId { get; set; }

    /// <summary>نام نمایشی سرور.</summary>
    public string Remark { get; set; } = string.Empty;

    // ----- اطلاعات اتصال پایه -----
    public Protocol Protocol { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }

    // ----- فیلدهای پروتکل‌اختصاصی -----
    /// <summary>VLESS/VMess: UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>VLESS: encryption (مثلاً none یا mlkem768...).</summary>
    public string? Encryption { get; set; }

    /// <summary>Trojan/Shadowsocks: گذرواژه.</summary>
    public string? Password { get; set; }

    /// <summary>Shadowsocks: cipher method (مثلاً aes-256-gcm).</summary>
    public string? Method { get; set; }

    /// <summary>VLESS: flow (مثلاً xtls-rprx-vision).</summary>
    public string? Flow { get; set; }

    /// <summary>VMess: alterId (معمولاً 0).</summary>
    public int AlterId { get; set; }

    // ----- لایه‌ی انتقال (streamSettings) -----
    public NetworkType Network { get; set; } = NetworkType.Raw;
    public SecurityType Security { get; set; } = SecurityType.None;

    /// <summary>TLS/REALITY: Server Name Indication.</summary>
    public string? Sni { get; set; }

    /// <summary>TLS/REALITY: اثر انگشت uTLS (مثلاً chrome).</summary>
    public string? Fingerprint { get; set; }

    /// <summary>TLS: ALPN (مثلاً h2,http/1.1).</summary>
    public string? Alpn { get; set; }

    /// <summary>REALITY: کلید عمومی (publicKey).</summary>
    public string? PublicKey { get; set; }

    /// <summary>REALITY: shortId.</summary>
    public string? ShortId { get; set; }

    /// <summary>REALITY: spiderX.</summary>
    public string? SpiderX { get; set; }

    /// <summary>WebSocket: مسیر.</summary>
    public string? WsPath { get; set; }

    /// <summary>WebSocket: هدر Host.</summary>
    public string? WsHost { get; set; }

    /// <summary>gRPC: serviceName.</summary>
    public string? GrpcServiceName { get; set; }

    /// <summary>gRPC: multiMode.</summary>
    public bool GrpcMultiMode { get; set; }

    /// <summary>HTTP伪装/HTTPUpgrade: host.</summary>
    public string? HttpHost { get; set; }

    /// <summary>HTTP伪装/HTTPUpgrade: مسیر.</summary>
    public string? HttpPath { get; set; }

    // ----- وضعیت و لینک اصلی -----
    /// <summary>لینک اشتراک اصلی (share link) — برای بازتولید و اشتراک‌گذاری.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>تأخیر اندازه‌گیری‌شده بر حسب میلی‌ثانیه (null = تست نشده/ناموفق).</summary>
    public int? LatencyMs { get; set; }

    /// <summary>آیا این سرور هم‌اکنون انتخاب/فعال است؟</summary>
    public bool IsActive { get; set; }

    /// <summary>زمان آخرین تست.</summary>
    public DateTime? LastTest { get; set; }

    /// <summary>زمان افزودن به لیست.</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>خلاصه‌ی کوتاه برای نمایش (آدرس:پورت).</summary>
    public string AddressSummary => $"{Address}:{Port}";
}
