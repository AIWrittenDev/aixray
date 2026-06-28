namespace AIXray.Core;

/// <summary>
/// یک گروه/سابسکریپشن که مجموعه‌ای از سرورها را در بر می‌گیرد.
/// </summary>
public class Group
{
    public long Id { get; set; }

    /// <summary>نام نمایشی گروه.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>آدرس سابسکریپشن (در صورت وجود).</summary>
    public string? SubscriptionUrl { get; set; }

    /// <summary>آیا آپدیت خودکار از سابسکریپشن فعال باشد؟</summary>
    public bool AutoUpdate { get; set; }

    /// <summary>بازه‌ی آپدیت خودکار بر حسب دقیقه.</summary>
    public int UpdateIntervalMinutes { get; set; } = 60;

    /// <summary>زمان آخرین آپدیت موفق.</summary>
    public DateTime? LastUpdate { get; set; }

    /// <summary>آیا این گروه پیش‌فرض (مثلاً «همه‌ی کانفیگ‌ها») است؟ قابل حذف نیست.</summary>
    public bool IsDefault { get; set; }
}
