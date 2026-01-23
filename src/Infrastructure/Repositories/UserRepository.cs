using AuthService.src.Application.Interfaces;
using Libs.Core.Public.src.DTOs.Requests;
using Libs.Core.Public.src.DTOs.Responses;
using Npgsql;

namespace AuthService.src.Infrastructure.Repositories;

public sealed class UserRepository(IPostgresDB db, ILogger<UserRepository> logger) : IUserRepository
{
    private readonly IPostgresDB _db = db;
    private readonly ILogger<UserRepository> _logger = logger;

    public async Task<RefreshTokenResponse?> GetValidRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        const string sql = @"SELECT user_id AS UserId
        FROM tbRefreshToken 
        WHERE refresh_token = @RefreshToken 
            AND revoked_at IS NULL 
            AND expires_at > NOW()
            LIMIT 1;";

        try
        {
            return await _db.QueryFirstOrDefaultAsync<RefreshTokenResponse?>(sql, new { RefreshToken = refreshToken }, ct);
        }
        catch (PostgresException pgEx)
        {
            _logger.LogError(pgEx, " - Unexpected PostgreSQL Error");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected error during transaction operation.");
            return null;
        }
    }

    public async Task<int> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            const string sql = @"UPDATE tbRefreshToken SET revoked_at = NOW() WHERE refresh_token = @RefreshToken;";

            var result = await _db.ExecuteAsync(sql, new { RefreshToken = refreshToken }, ct);

            return result == 0
                ? 0
                : 1;
        }
        catch (PostgresException pgEx)
        {
            _logger.LogError(pgEx, " - Unexpected PostgreSQL Error");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected error during transaction operation.");
            return 0;
        }
    }

    public async Task<int> SaveRefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            const string sql = @"INSERT INTO tbRefreshToken (user_id, refresh_token) VALUES (@UserId, @RefreshToken);";

            var result = await _db.ExecuteAsync(sql, request, ct);

            return result == 0
                ? 0
                : 1;
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogError(pgEx, " - Already exists.");
            return 2;
        }
        catch (PostgresException pgEx)
        {
            _logger.LogError(pgEx, " - Unexpected PostgreSQL Error");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected error during transaction operation.");
            return 0;
        }
    }
}