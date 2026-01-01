using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace AuthService.src.Repositories
{
    public class AuthRepository(IPostgresDbData db, IConfiguration config, ILogger<AuthRepository> logger) : IAuthRepository
    {
        private readonly IPostgresDbData _db = db;
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthRepository> _logger = logger;

        public string GenerateAccessToken(AuthUserDTO user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSettings:securityKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWTSettings:validIssuer"],
                audience: _config["JWTSettings:validAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshToken(Guid userId)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(randomBytes);

            var refreshToken = new RefreshTokenDTO
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMonths(2)
            };

            await SaveRefreshToken(refreshToken);
            return token;
        }

        private async Task SaveRefreshToken(RefreshTokenDTO token)
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