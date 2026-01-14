using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;

namespace AuthService.src.Application.Interfaces;

public interface IUserRepository
{
    Task<GetTokenResponse?> GetValidRefreshToken(string refreshToken, CancellationToken ct);
    Task<int> RevokeRefreshToken(string refreshToken, CancellationToken ct);
    Task<int> SaveRefreshToken(RefreshTokenRequest request, CancellationToken ct);
}