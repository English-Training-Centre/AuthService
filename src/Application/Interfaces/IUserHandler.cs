using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;

namespace AuthService.src.Application.Interfaces;

public interface IUserHandler
{
    Task<SignInResponse> SignIn(UserAuthRequest request, CancellationToken ct);
    Task<UserRefreshTokenResponse> RefreshToken(RefreshTokenRequest request, CancellationToken ct);
    Task<CheckSessionResponse> CheckSession(Guid userId, CancellationToken ct);
    string GenerateAccessToken(GenerateTokenRequest request);
    Task<string> GenerateRefreshToken(Guid userId, CancellationToken ct);
    Task<RefreshTokenResponse?> GetValidRefreshToken(string token, CancellationToken ct);
    Task RevokeRefreshToken(string token, CancellationToken ct);
}