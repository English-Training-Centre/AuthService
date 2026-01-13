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

    public async Task<SignInResponse> SignIn(UserAuthRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.AuthAsync(request, ct);

            if (result.IsSuccess == false || result.UserId is null || result.Username is null || result.Role is null)
            {
                _logger.LogWarning($"SignIn Warning... Something is null\nIsSuccess: {result.IsSuccess}\nUserId: {result.UserId}\nUsername: {request.Username}\nRole: {result.Role}");
                return SignInResponse.Failure();
            }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                Username = result.Username,
                Role = result.Role
            };

            var accessToken = GenerateAccessToken(tokenRequest);
            var refreshToken = await GenerateRefreshToken(tokenRequest.UserId, ct);

            return new SignInResponse
            {
                IsSuccess = result.IsSuccess,
                UserId = tokenRequest.UserId,
                Role = tokenRequest.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error 503");
            return SignInResponse.Failure();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected Error");
            return SignInResponse.Failure();
        }
    }

    public async Task<UserRefreshTokenResponse> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.GetAuthByIdAsync(request.UserId, ct);
            if (result.IsSuccess == false || result.UserId is null || result.Username is null || result.Role is null) { return UserRefreshTokenResponse.Failure(); }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                Username = result.Username,
                Role = result.Role
            };

            var newAccessToken = GenerateAccessToken(tokenRequest);
            var newRefreshToken = await GenerateRefreshToken(tokenRequest.UserId, ct);

            await RevokeRefreshToken(request.Token, ct);

            return new UserRefreshTokenResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session expired. Please sign in again.");
            return UserRefreshTokenResponse.Failure();
        }
    }

    public async Task<CheckSessionResponse> CheckSession(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _userGrpcServiceClient.GetAuthByIdAsync(userId, ct);

            if (result.IsSuccess == false || result.UserId is null || result.Username is null || result.Role is null) { return CheckSessionResponse.Failure(); }

            var tokenRequest = new GenerateTokenRequest
            {
                UserId = result.UserId.Value,
                Username = result.Username,
                Role = result.Role
            };

            var newAccessToken = GenerateAccessToken(tokenRequest);

            return new CheckSessionResponse
            {
                IsSuccess = result.IsSuccess,
                UserId = tokenRequest.UserId,
                Role = tokenRequest.Role,
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
                expires: DateTime.UtcNow.AddMinutes(30),
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
            var token = Convert.ToBase64String(randomBytes);

            var newToken = new RefreshTokenRequest
            (
                userId,
                token
            );

            await _userRepository.SaveRefreshToken(newToken, ct);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " - Unexpected GenerateRefreshToken Error");
            return "";
        }
    }

    public async Task<RefreshTokenResponse?> GetValidRefreshToken(string token, CancellationToken ct)
    {
        if (token is null) return null;

        return await _userRepository.GetValidRefreshToken(token, ct);
    }

    public async Task RevokeRefreshToken(string token, CancellationToken ct)
    {
        if (token is null) return;

        await _userRepository.RevokeRefreshToken(token, ct);
    }
}