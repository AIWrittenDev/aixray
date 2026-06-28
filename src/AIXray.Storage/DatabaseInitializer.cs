using Dapper;
using Microsoft.Data.Sqlite;

namespace AIXray.Storage;

/// <summary>
/// ایجاد و مهاجرت اولیه‌ی جداول دیتابیس.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DatabaseInitializer(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS groups (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT    NOT NULL,
                subscription_url   TEXT,
                auto_update INTEGER NOT NULL DEFAULT 0,
                update_interval_min INTEGER NOT NULL DEFAULT 60,
                last_update TEXT,
                is_default  INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS servers (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                group_id    INTEGER,
                remark      TEXT    NOT NULL DEFAULT '',
                protocol    TEXT    NOT NULL,
                address     TEXT    NOT NULL,
                port        INTEGER NOT NULL,
                uuid        TEXT,
                encryption  TEXT,
                password    TEXT,
                method      TEXT,
                flow        TEXT,
                alter_id    INTEGER NOT NULL DEFAULT 0,
                network     TEXT    NOT NULL DEFAULT 'raw',
                security    TEXT    NOT NULL DEFAULT 'none',
                sni         TEXT,
                fingerprint TEXT,
                alpn        TEXT,
                public_key  TEXT,
                short_id    TEXT,
                spider_x    TEXT,
                ws_path     TEXT,
                ws_host     TEXT,
                grpc_service_name  TEXT,
                grpc_multi_mode    INTEGER NOT NULL DEFAULT 0,
                http_host   TEXT,
                http_path   TEXT,
                url         TEXT    NOT NULL DEFAULT '',
                latency_ms  INTEGER,
                is_active   INTEGER NOT NULL DEFAULT 0,
                last_test   TEXT,
                added_at    TEXT    NOT NULL,
                FOREIGN KEY (group_id) REFERENCES groups(id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS settings (
                id             INTEGER PRIMARY KEY CHECK (id = 1),
                log_level      TEXT    NOT NULL DEFAULT 'warning',
                local_port     INTEGER NOT NULL DEFAULT 10808,
                share_local    INTEGER NOT NULL DEFAULT 0,
                mode           TEXT    NOT NULL DEFAULT 'SystemProxy',
                language       TEXT    NOT NULL DEFAULT 'fa',
                xray_binary_path TEXT,
                auto_connect   INTEGER NOT NULL DEFAULT 0
            );
        """);

        // ردیف تنظیمات پیش‌فرض اگر وجود ندارد
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings WHERE id = 1");
        if (count == 0)
        {
            await conn.ExecuteAsync("""
                INSERT INTO settings (id, log_level, local_port, share_local, mode, language, auto_connect)
                VALUES (1, 'warning', 10808, 0, 'SystemProxy', 'fa', 0);
            """);
        }
    }
}
