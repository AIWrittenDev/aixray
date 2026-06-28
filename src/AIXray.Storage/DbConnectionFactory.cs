using Microsoft.Data.Sqlite;

namespace AIXray.Storage;

/// <summary>
/// فابریک ایجاد اتصال SQLite. مسیر دیتابیس در دایرکتوری داده‌ی برنامه قرار می‌گیرد.
/// </summary>
public interface IDbConnectionFactory
{
    SqliteConnection CreateConnection();
}

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string dbPath)
    {
        // پوشه‌ی دایرکتوری ایجاد شود اگر وجود ندارد
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
