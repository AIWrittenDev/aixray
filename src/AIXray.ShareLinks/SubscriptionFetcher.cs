using System.Text;

namespace AIXray.ShareLinks;

/// <summary>
/// دریافت و decode محتوای سابسکریپشن از URL.
/// محتوا ممکن است plain (چند خط لینک) یا base64 باشد.
/// </summary>
public interface ISubscriptionFetcher
{
    /// <summary>دریافت لیست لینک‌ها از آدرس سابسکریپشن.</summary>
    Task<string> FetchRawContentAsync(string subscriptionUrl);

    /// <summary>دریافت + decode محتوای سابسکریپشن (بررسی base64).</summary>
    Task<string> FetchAndDecodeAsync(string subscriptionUrl);
}

public class SubscriptionFetcher : ISubscriptionFetcher
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "AIXray/1.0" },
            // Many subscription services require this header
            { "Accept", "*/*" },
        },
    };

    /// <summary>
    /// ساخت با proxy خاص (مثلاً وقتی می‌خواهد از طریق سرور متصل، سابسکریپشن آپدیت کند).
    /// </summary>
    public SubscriptionFetcher(string? proxyUrl = null)
    {
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            // ایجاد یک HttpClient جدید با proxy
            var handler = new System.Net.Http.HttpClientHandler
            {
                Proxy = new System.Net.WebProxy(proxyUrl),
                UseProxy = true,
            };
            _http = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        }
        else
        {
            _http = Http;
        }
    }

    private readonly HttpClient _http;

    public async Task<string> FetchRawContentAsync(string subscriptionUrl)
    {
        using var response = await _http.GetAsync(subscriptionUrl);
        response.EnsureSuccessStatusCode();

        // بعضی سرورها gzip می‌کنند — HttpClient خودش handle می‌کند
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> FetchAndDecodeAsync(string subscriptionUrl)
    {
        var raw = await FetchRawContentAsync(subscriptionUrl);

        // بررسی اینکه آیا محتوا base64 است (کمتر از 5 خط و فقط کاراکترهای base64)
        if (IsBase64(raw))
        {
            try
            {
                var decoded = DecodeBase64(raw);
                // اگر decode شده شامل // بود، یعنی base64 واقعی بوده
                if (decoded.Contains("://", StringComparison.OrdinalIgnoreCase))
                    return decoded;
            }
            catch
            {
                // اگر base64 decode شکست خورد، raw برمی‌گردد
            }
        }

        return raw;
    }

    private static bool IsBase64(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // اگر همه‌ی خط‌ها طول مناسبی برای base64 دارند و کاراکتر نامعتبر ندارند
        return lines.Length <= 50 && lines.All(line =>
            line.Length > 10 &&
            line.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=' || c == '-' || c == '_'));
    }

    private static string DecodeBase64(string input)
    {
        input = input.Trim().Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        var bytes = Convert.FromBase64String(input);
        return Encoding.UTF8.GetString(bytes);
    }
}
