using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Npgsql;

namespace AuthService.src.Repositories
{
    public class AuthRepository(IPostgresDbData db, ILogger<AuthRepository> logger) : IAuthRepository
    {
        private readonly IPostgresDbData _db = db;        
        private readonly ILogger<AuthRepository> _logger = logger;

        public async Task<RefreshTokenDTO?> GetValidRefreshToken(string token)
        {
            const string sql = @"SELECT
                id AS Id, 
                user_id AS UserId, 
                token AS Token, 
                expires_at AS ExpiresAt, 
                created_at AS CreatedAt, 
                revoked_at AS RevokedAt 
            FROM tbRefreshToken 
            WHERE token = @Token 
                AND revoked_at IS NULL 
                AND expires_at > NOW();";
            try
            {
                return await _db.QueryFirstOrDefaultAsync<RefreshTokenDTO>(sql, new { Token = token });
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

        public async Task RevokeRefreshToken(string token)
        {
            try
            {
                const string sql = @"UPDATE tbRefreshToken SET revoked_at = NOW() WHERE token = @Token;";

                await _db.ExecuteAsync(sql, new { Token = token });
            }
            catch (PostgresException pgEx)
            {      
                _logger.LogError(pgEx, " - Unexpected PostgreSQL Error");             
            }
            catch (Exception ex)
            {  
                _logger.LogError(ex, " - Unexpected error during transaction operation.");
            }
        }

        public async Task SaveRefreshToken(RefreshTokenDTO token)
        {
            try
            {
                const string sql = @"INSERT INTO tbRefreshToken (id, user_id, token, expires_at) VALUES (@Id, @UserId, @Token, @ExpiresAt);";

                await _db.ExecuteAsync(sql, token);
            }
            catch (PostgresException pgEx)
            {      
                _logger.LogError(pgEx, " - Unexpected PostgreSQL Error");             
            }
            catch (Exception ex)
            {  
                _logger.LogError(ex, " - Unexpected error during transaction operation.");
            }
        }
    }
}