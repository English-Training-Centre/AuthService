using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;
using AuthService.src.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.src.Application.Handlers;

public sealed class UserHandler(IUserRepository userRepository, IUserGrpcServiceClient userGrpcServiceClient, ILogger<UserHandler> logger, IConfiguration config) : IUserHandler
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserGrpcServiceClient _userGrpcServiceClient = userGrpcServiceClient;
    private readonly ILogger<UserHandler> _logger = logger;
    private readonly IConfiguration _config = config;

    public async Task<AuthResponse> SignIn(UserAuthRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.AuthAsync(request, ct);

            if (result.IsSuccess == false || result.UserId is null || result.FullName is null || result.Username is null || result.Role is null)
            {
                _logger.LogInformation($"Invalid credentials.");
                return AuthResponse.Failure();
            }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                FullName = result.FullName,
                Username = result.Username,
                Role = result.Role
            };

            var accessToken = GenerateAccessToken(tokenRequest);
            var refreshToken = await GenerateRefreshToken(tokenRequest.UserId, ct);

            return new AuthResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error 503");
            return AuthResponse.Failure();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - An unexpected error occurred...");
            return AuthResponse.Failure();
        }
    }

    public async Task<AuthResponse> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.GetAuthByIdAsync(request.UserId, ct);
            if (result.IsSuccess == false || result.UserId is null || result.FullName is null || result.Username is null || result.Role is null) { return AuthResponse.Failure(); }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                FullName = result.FullName,
                Username = result.Username,
                Role = result.Role
            };

            var newAccessToken = GenerateAccessToken(tokenRequest);
            var newRefreshToken = await GenerateRefreshToken(tokenRequest.UserId, ct);

            await RevokeRefreshToken(request.RefreshToken, ct);

            return new AuthResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: UserHandler -> RefreshTokn(....)");
            return AuthResponse.Failure();
        }
    }

    public async Task<CheckSessionResponse> CheckSession(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.GetAuthByIdAsync(userId, ct);

            if (result.IsSuccess == false || result.UserId is null || result.FullName is null || result.Username is null || result.Role is null) { return CheckSessionResponse.Failure(); }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                FullName = result.FullName,
                Username = result.Username,
                Role = result.Role
            };

            var newAccessToken = GenerateAccessToken(tokenRequest);

            return new CheckSessionResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = newAccessToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session expired. Please sign in again.");
            return CheckSessionResponse.Failure();
        }
    }

    public string GenerateAccessToken(GenerateTokenRequest request)
    {
        try
        {
            var claims = new[]
            {
                    new Claim(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, request.FullName),
                    new Claim(JwtRegisteredClaimNames.UniqueName, request.Username),
                    new Claim(ClaimTypes.Role, request.Role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSettings:securityKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWTSettings:validIssuer"],
                audience: _config["JWTSettings:validAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var newToken = new JwtSecurityTokenHandler().WriteToken(token);
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected GenerateAccessToken Error");
            return "";
        }
    }

    public async Task<string> GenerateRefreshToken(Guid userId, CancellationToken ct)
    {
        try
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(randomBytes);

            var newRefreshToken = new RefreshTokenRequest
            (
                userId,
                refreshToken
            );

            await _userRepository.SaveRefreshToken(newRefreshToken, ct);
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected GenerateRefreshToken Error");
            return "";
        }
    }

    public async Task<GetTokenResponse?> GetValidRefreshToken(string refreshToken, CancellationToken ct)
    {
        if (refreshToken is null) return null;

        return await _userRepository.GetValidRefreshToken(refreshToken, ct);
    }

    public async Task RevokeRefreshToken(string refreshToken, CancellationToken ct)
    {
        if (refreshToken is null) return;

        await _userRepository.RevokeRefreshToken(refreshToken, ct);
    }
}