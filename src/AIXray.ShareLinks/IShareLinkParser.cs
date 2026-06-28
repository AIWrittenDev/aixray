using AIXray.Core;

namespace AIXray.ShareLinks;

/// <summary>
/// پارس‌کننده‌ی لینک‌های اشتراک xray/v2ray.
/// هر پارس‌کننده مسئول تبدیل یک لینک (string) به مدل <see cref="Server"/> است.
/// </summary>
public interface IShareLinkParser
{
    /// <summary>پروتکلی که این پارس‌کننده مدیریت می‌کند.</summary>
    Protocol Protocol { get; }

    /// <summary>پیشوند لینک (مثلاً "vless://").</summary>
    string Scheme { get; }

    /// <summary>
    /// تلاش برای پارس لینک. در صورت ناموفق بودن، null برمی‌گرداند (خطا throw نمی‌کند).
    /// </summary>
    Server? TryParse(string link);
}

/// <summary>
/// پارس‌کننده‌ی مرکزی — لینک‌ها را بر اساس پیشوند به پارس‌کننده‌ی مناسب ارسال می‌کند.
/// </summary>
public interface IShareLinkParserService
{
    /// <summary>پارس یک لینک تکی.</summary>
    Server? ParseLink(string link);

    /// <summary>پارس چند لینک (هر خط یک لینک). لینک‌های نامعتبر نادیده گرفته می‌شوند.</summary>
    List<Server> ParseLinks(string content);
}
