using AuthService.src.Application.DTOs.Requests;
using AuthService.src.Application.DTOs.Responses;

namespace AuthService.src.Application.Interfaces;

public interface IUserRepository
{
    Task<GetTokenResponse?> GetValidRefreshToken(string refreshToken, CancellationToken ct);
    Task<int> RevokeRefreshToken(string refreshToken, CancellationToken ct);
    Task<int> SaveRefreshToken(RefreshTokenRequest request, CancellationToken ct);
}