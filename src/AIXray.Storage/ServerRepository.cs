using AIXray.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AIXray.Storage;

/// <summary>
/// مخزن سرورها — خواندن، نوشتن، حذف و فعال‌سازی سرورها.
/// </summary>
public interface IServerRepository
{
    Task<List<Server>> GetAllAsync();
    Task<List<Server>> GetByGroupAsync(long groupId);
    Task<Server?> GetByIdAsync(long id);
    Task<Server> AddAsync(Server server);
    Task UpdateAsync(Server server);
    Task DeleteAsync(long id);
    Task SetActiveAsync(long id, bool active);
    Task DeactivateAllAsync();
}

public class ServerRepository : IServerRepository
{
    private readonly IDbConnectionFactory _factory;

    public ServerRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Server>> GetAllAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync(ServerSelectSql());
        return rows.Select(MapRowToServer).ToList();
    }

    public async Task<List<Server>> GetByGroupAsync(long groupId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync(ServerSelectSql() + " WHERE s.group_id = @GroupId", new { GroupId = groupId });
        return rows.Select(MapRowToServer).ToList();
    }

    public async Task<Server?> GetByIdAsync(long id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var row = await conn.QueryFirstOrDefaultAsync(ServerSelectSql() + " WHERE s.id = @Id", new { Id = id });
        return row == null ? null : MapRowToServer(row);
    }

    public async Task<Server> AddAsync(Server server)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = """
            INSERT INTO servers (
                group_id, remark, protocol, address, port,
                uuid, encryption, password, method, flow, alter_id,
                network, security, sni, fingerprint, alpn, public_key, short_id, spider_x,
                ws_path, ws_host, grpc_service_name, grpc_multi_mode,
                http_host, http_path, url, latency_ms, is_active, last_test, added_at
            ) VALUES (
                @GroupId, @Remark, @Protocol, @Address, @Port,
                @Uuid, @Encryption, @Password, @Method, @Flow, @AlterId,
                @Network, @Security, @Sni, @Fingerprint, @Alpn, @PublicKey, @ShortId, @SpiderX,
                @WsPath, @WsHost, @GrpcServiceName, @GrpcMultiMode,
                @HttpHost, @HttpPath, @Url, @LatencyMs, @IsActive, @LastTest, @AddedAt
            );
            SELECT LAST_INSERT_ROWID();
        """;
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            server.GroupId, server.Remark,
            Protocol = server.Protocol.ToXrayName(),
            server.Address, server.Port,
            server.Uuid, server.Encryption, server.Password, server.Method, server.Flow, server.AlterId,
            Network = server.Network.ToXrayName(),
            Security = server.Security.ToXrayName(),
            server.Sni, server.Fingerprint, server.Alpn, server.PublicKey, server.ShortId, server.SpiderX,
            server.WsPath, server.WsHost, server.GrpcServiceName, server.GrpcMultiMode,
            server.HttpHost, server.HttpPath, server.Url, server.LatencyMs,
            IsActive = server.IsActive ? 1 : 0,
            LastTest = server.LastTest?.ToString("o"),
            AddedAt = server.AddedAt.ToString("o"),
        });
        server.Id = id;
        return server;
    }

    public async Task UpdateAsync(Server server)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync("""
            UPDATE servers SET
                group_id = @GroupId, remark = @Remark, protocol = @Protocol,
                address = @Address, port = @Port,
                uuid = @Uuid, encryption = @Encryption, password = @Password,
                method = @Method, flow = @Flow, alter_id = @AlterId,
                network = @Network, security = @Security,
                sni = @Sni, fingerprint = @Fingerprint, alpn = @Alpn,
                public_key = @PublicKey, short_id = @ShortId, spider_x = @SpiderX,
                ws_path = @WsPath, ws_host = @WsHost,
                grpc_service_name = @GrpcServiceName, grpc_multi_mode = @GrpcMultiMode,
                http_host = @HttpHost, http_path = @HttpPath,
                url = @Url, latency_ms = @LatencyMs, is_active = @IsActive,
                last_test = @LastTest, added_at = @AddedAt
            WHERE id = @Id
        """, new
        {
            server.Id, server.GroupId, server.Remark,
            Protocol = server.Protocol.ToXrayName(),
            server.Address, server.Port,
            server.Uuid, server.Encryption, server.Password, server.Method, server.Flow, server.AlterId,
            Network = server.Network.ToXrayName(),
            Security = server.Security.ToXrayName(),
            server.Sni, server.Fingerprint, server.Alpn, server.PublicKey, server.ShortId, server.SpiderX,
            server.WsPath, server.WsHost, server.GrpcServiceName, server.GrpcMultiMode,
            server.HttpHost, server.HttpPath, server.Url, server.LatencyMs,
            IsActive = server.IsActive ? 1 : 0,
            LastTest = server.LastTest?.ToString("o"),
            AddedAt = server.AddedAt.ToString("o"),
        });
    }

    public async Task DeleteAsync(long id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("DELETE FROM servers WHERE id = @Id", new { Id = id });
    }

    public async Task SetActiveAsync(long id, bool active)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("UPDATE servers SET is_active = @Active WHERE id = @Id",
            new { Id = id, Active = active ? 1 : 0 });
    }

    public async Task DeactivateAllAsync()
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync("UPDATE servers SET is_active = 0");
    }

    // ----- helpers -----
    private static string ServerSelectSql() => """
        SELECT
            s.id, s.group_id, s.remark, s.protocol, s.address, s.port,
            s.uuid, s.encryption, s.password, s.method, s.flow, s.alter_id,
            s.network, s.security,
            s.sni, s.fingerprint, s.alpn, s.public_key, s.short_id, s.spider_x,
            s.ws_path, s.ws_host, s.grpc_service_name, s.grpc_multi_mode,
            s.http_host, s.http_path,
            s.url, s.latency_ms, s.is_active, s.last_test, s.added_at
        FROM servers s
    """;

    private static Server MapRowToServer(dynamic r)
    {
        return new Server
        {
            Id = r.id,
            GroupId = r.group_id as long?,
            Remark = r.remark ?? "",
            Protocol = EnumMappings.ProtocolFromName(r.protocol),
            Address = r.address ?? "",
            Port = r.port,
            Uuid = r.uuid,
            Encryption = r.encryption,
            Password = r.password,
            Method = r.method,
            Flow = r.flow,
            AlterId = r.alter_id,
            Network = EnumMappings.NetworkFromName(r.network),
            Security = EnumMappings.SecurityFromName(r.security),
            Sni = r.sni,
            Fingerprint = r.fingerprint,
            Alpn = r.alpn,
            PublicKey = r.public_key,
            ShortId = r.short_id,
            SpiderX = r.spider_x,
            WsPath = r.ws_path,
            WsHost = r.ws_host,
            GrpcServiceName = r.grpc_service_name,
            GrpcMultiMode = r.grpc_multi_mode != 0,
            HttpHost = r.http_host,
            HttpPath = r.http_path,
            Url = r.url ?? "",
            LatencyMs = r.latency_ms as int?,
            IsActive = r.is_active != 0,
            LastTest = ParseDateTime(r.last_test),
            AddedAt = ParseDateTime(r.added_at) ?? DateTime.UtcNow,
        };
    }

    private static DateTime? ParseDateTime(object? value)
    {
        if (value == null || value is DBNull) return null;
        if (value is DateTime dt) return dt;
        if (DateTime.TryParse(value.ToString(), out var result)) return result;
        return null;
    }
}
