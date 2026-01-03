using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.src.Handlers
{
    public sealed class AuthHandler(IAuthRepository authRepository, IUserServices userServices, IConfiguration config, ILogger<AuthHandler> logger) : IAuthHandler
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly IUserServices _userServices = userServices;
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthHandler> _logger = logger;

        public async Task<UserResponseDTO> SignIn(AuthRequestDTO auth)
        {
            try
            {
                var result = await _userServices.AuthUserAsync(auth);

                if (result is null || result.IsSuccess == false)
                {
                    return UserResponseDTO.Failure();
                }

                var user = new AuthUserDTO
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Role = result.Role
                };
                
                var accessToken = GenerateAccessToken(user);
                var refreshToken = await GenerateRefreshToken(user.Id);

                return new UserResponseDTO
                {
                    IsSuccess = result.IsSuccess,
                    UserId = result.UserId,
                    Role = result.Role,                    
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error 503");
                return UserResponseDTO.Failure();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Unexpected Error");
                return UserResponseDTO.Failure();
            }
        }

        public async Task<UserRefreshTokenDTO> RefreshToken(Guid userId, string refreshToken)
        {
            try
            {
                var result = await _userServices.GetUserByIdAsync(userId);
                if (result is null || result.IsSuccess == false) { return UserRefreshTokenDTO.Failure(); }

                var user = new AuthUserDTO
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Role = result.Role
                };

                var newAccessToken = GenerateAccessToken(user);
                var newRefreshToken = await GenerateRefreshToken(user.Id);

                await RevokeRefreshToken(refreshToken);

                return new UserRefreshTokenDTO
                {
                    IsSuccess = result.IsSuccess,              
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session expired. Please sign in again.");
                return UserRefreshTokenDTO.Failure();
            }
        }

        public async Task<AuthResponseDTO> CheckSession(Guid userId)
        {
            try
            {
                var result = await _userServices.GetUserByIdAsync(userId);
                if (result == null || result.IsSuccess == false)
                {
                    return AuthResponseDTO.Failure();
                }

                var user = new AuthUserDTO
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Role = result.Role
                };

                var newAccessToken = GenerateAccessToken(user);

                return new AuthResponseDTO
                {
                    IsSuccess = result.IsSuccess,
                    UserId = result.UserId,
                    Role = result.Role,
                    AccessToken = newAccessToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session expired. Please sign in again.");
                return AuthResponseDTO.Failure();
            }
        }

        public string GenerateAccessToken(AuthUserDTO user)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, " - Unexpected GenerateAccessToken Error");
                return "";
            }
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

            await _authRepository.SaveRefreshToken(refreshToken);
            return token;
        }

        public async Task<RefreshTokenDTO?> GetValidRefreshToken(string token)
        {
            if (token is null) return null;

            return await _authRepository.GetValidRefreshToken(token);
        }

        public async Task RevokeRefreshToken(string token)
        {
            if (token is null) return;

            await _authRepository.RevokeRefreshToken(token);
        }
    }
}