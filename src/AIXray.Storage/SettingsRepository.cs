using AIXray.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AIXray.Storage;

/// <summary>
/// مخزن تنظیمات — یک ردیف واحد (id=1).
/// </summary>
public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionFactory _factory;

    public SettingsRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AppSettings> LoadAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var row = await conn.QueryFirstOrDefaultAsync("""
            SELECT log_level, local_port, share_local, mode, language, xray_binary_path, auto_connect
            FROM settings WHERE id = 1
        """);
        if (row == null) return new AppSettings();

        return new AppSettings
        {
            LogLevel = EnumMappings.LogLevelFromName(row.log_level),
            LocalPort = row.local_port,
            ShareLocal = row.share_local != 0,
            Mode = EnumFromName(row.mode),
            Language = EnumFromLanguage(row.language),
            XrayBinaryPath = row.xray_binary_path,
            AutoConnect = row.auto_connect != 0,
        };
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("""
            UPDATE settings SET
                log_level = @LogLevel,
                local_port = @LocalPort,
                share_local = @ShareLocal,
                mode = @Mode,
                language = @Language,
                xray_binary_path = @XrayBinaryPath,
                auto_connect = @AutoConnect
            WHERE id = 1
        """, new
        {
            LogLevel = settings.LogLevel.ToXrayName(),
            LocalPort = settings.LocalPort,
            ShareLocal = settings.ShareLocal ? 1 : 0,
            Mode = settings.Mode.ToString(),
            Language = settings.Language.ToString().ToLowerInvariant(),
            settings.XrayBinaryPath,
            AutoConnect = settings.AutoConnect ? 1 : 0,
        });
    }

    private static ConnectionMode EnumFromName(string? name) => (name ?? "") switch
    {
        "Direct" => ConnectionMode.Direct,
        "Tun" => ConnectionMode.Tun,
        _ => ConnectionMode.SystemProxy,
    };

    private static AppLanguage EnumFromLanguage(string? name) => (name ?? "") switch
    {
        "en" => AppLanguage.En,
        _ => AppLanguage.Fa,
    };
}
