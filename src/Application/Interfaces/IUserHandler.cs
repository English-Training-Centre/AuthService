using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;

namespace AuthService.src.Application.Interfaces;

public interface IUserHandler
{
    Task<AuthResponse> SignIn(UserAuthRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshToken(RefreshTokenRequest request, CancellationToken ct);
    Task<CheckSessionResponse> CheckSession(Guid userId, CancellationToken ct);
    string GenerateAccessToken(GenerateTokenRequest request);
    Task<string> GenerateRefreshToken(Guid userId, CancellationToken ct);
    Task<GetTokenResponse?> GetValidRefreshToken(string refreshToken, CancellationToken ct);
    Task RevokeRefreshToken(string refreshToken, CancellationToken ct);
}