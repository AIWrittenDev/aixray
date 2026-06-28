using AIXray.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AIXray.Storage;

/// <summary>
/// مخزن گروه‌ها/سابسکریپشن‌ها.
/// </summary>
public interface IGroupRepository
{
    Task<List<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(long id);
    Task<Group> AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(long id);
}

public class GroupRepository : IGroupRepository
{
    private readonly IDbConnectionFactory _factory;

    public GroupRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Group>> GetAllAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync("""
            SELECT id, name, subscription_url, auto_update, update_interval_min, last_update, is_default
            FROM groups ORDER BY is_default DESC, name ASC
        """);
        return rows.Select(MapRow).ToList();
    }

    public async Task<Group?> GetByIdAsync(long id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var row = await conn.QueryFirstOrDefaultAsync("""
            SELECT id, name, subscription_url, auto_update, update_interval_min, last_update, is_default
            FROM groups WHERE id = @Id
        """, new { Id = id });
        return row == null ? null : MapRow(row);
    }

    public async Task<Group> AddAsync(Group group)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var sql = """
            INSERT INTO groups (name, subscription_url, auto_update, update_interval_min, is_default)
            VALUES (@Name, @SubscriptionUrl, @AutoUpdate, @UpdateIntervalMin, @IsDefault);
            SELECT LAST_INSERT_ROWID();
        """;
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            group.Name, group.SubscriptionUrl,
            AutoUpdate = group.AutoUpdate ? 1 : 0,
            group.UpdateIntervalMinutes,
            IsDefault = group.IsDefault ? 1 : 0,
        });
        group.Id = id;
        return group;
    }

    public async Task UpdateAsync(Group group)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("""
            UPDATE groups SET
                name = @Name, subscription_url = @SubscriptionUrl,
                auto_update = @AutoUpdate, update_interval_min = @UpdateIntervalMin,
                last_update = @LastUpdate, is_default = @IsDefault
            WHERE id = @Id
        """, new
        {
            group.Id, group.Name, group.SubscriptionUrl,
            AutoUpdate = group.AutoUpdate ? 1 : 0,
            UpdateIntervalMin = group.UpdateIntervalMinutes,
            LastUpdate = group.LastUpdate?.ToString("o"),
            IsDefault = group.IsDefault ? 1 : 0,
        });
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("DELETE FROM groups WHERE id = @Id AND is_default = 0", new { Id = id });
    }

    private static Group MapRow(dynamic r) => new()
    {
        Id = r.id,
        Name = r.name ?? "",
        SubscriptionUrl = r.subscription_url,
        AutoUpdate = r.auto_update != 0,
        UpdateIntervalMinutes = r.update_interval_min,
        LastUpdate = ParseDateTime(r.last_update),
        IsDefault = r.is_default != 0,
    };

    private static DateTime? ParseDateTime(object? value)
    {
        if (value == null || value is DBNull) return null;
        if (value is DateTime dt) return dt;
        if (DateTime.TryParse(value.ToString(), out var result)) return result;
        return null;
    }
}
