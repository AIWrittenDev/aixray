using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AIXray.Xray;

/// <summary>
/// دانلود و مدیریت فایل اجرایی xray-core از GitHub releases.
/// اولین اجرا → بررسی وجود فایل → دانلود آخرین نسخه‌ی ویندوز 64-bit.
/// </summary>
public interface IXrayDownloader
{
    /// <summary>مسیر دایرکتوری نصب xray (شامل xray.exe، geoip.dat، geosite.dat).</summary>
    string InstallPath { get; }

    /// <summary>مسیر کامل xray.exe.</summary>
    string XrayExePath { get; }

    /// <summary>آیا xray.exe در مسیر نصب وجود دارد؟</summary>
    bool IsInstalled { get; }

    /// <summary>بررسی وجود → دانلود (در صورت نیاز).</summary>
    Task EnsureInstalledAsync(IProgress<string>? progress = null);
}

public class XrayDownloader : IXrayDownloader
{
    private const string GitHubRepo = "XTLS/Xray-core";
    private const string AssetPattern = "Xray-windows-64.zip";
    private const string XrayExe = "xray.exe";
    private const string GeoipDat = "geoip.dat";
    private const string GeositeDat = "geosite.dat";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
        DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse("AIXray/1.0") } },
    };

    private readonly string _installPath;

    public XrayDownloader(string? installPath = null)
    {
        _installPath = installPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIXray", "runtime");
    }

    public string InstallPath => _installPath;
    public string XrayExePath => Path.Combine(_installPath, XrayExe);
    public bool IsInstalled => File.Exists(XrayExePath);

    public async Task EnsureInstalledAsync(IProgress<string>? progress = null)
    {
        if (IsInstalled)
        {
            progress?.Report("xray-core already installed.");
            return;
        }

        progress?.Report("Fetching latest release info...");
        var downloadUrl = await GetLatestAssetUrlAsync();

        progress?.Report($"Downloading xray-core from: {downloadUrl}");
        Directory.CreateDirectory(_installPath);

        using var response = await Http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var tempZip = Path.Combine(_installPath, "xray-latest.zip");

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(tempZip);
        var buffer = new byte[81920];
        long downloaded = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (totalBytes > 0)
            {
                var percent = (int)(downloaded * 100 / totalBytes);
                progress?.Report($"Downloading... {percent}% ({downloaded / 1024 / 1024:F1} MB)");
            }
        }

        progress?.Report("Extracting archive...");
        ZipFile.ExtractToDirectory(tempZip, _installPath, overwriteFiles: true);
        File.Delete(tempZip);

        // بررسی وجود فایل‌های ضروری
        if (!File.Exists(XrayExePath))
            throw new FileNotFoundException($"xray.exe not found after extraction in {_installPath}");

        progress?.Report("xray-core installed successfully.");
    }

    private static async Task<string> GetLatestAssetUrlAsync()
    {
        var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var response = await Http.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Rate limited — 10 ثانیه صبر کن
                    await Task.Delay(10000);
                    continue;
                }
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var assets = doc.RootElement.GetProperty("assets");

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Contains("windows-64", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        return asset.GetProperty("browser_download_url").GetString()
                               ?? throw new InvalidOperationException("Download URL not found.");
                    }
                }

                throw new FileNotFoundException(
                    $"No matching asset ({AssetPattern}) found in latest release.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse GitHub API response: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException(
            "Failed to fetch release info after 3 attempts (possibly rate-limited).");
    }
}
