using Libs.Core.Public.src.DTOs.Requests;
using Libs.Core.Public.src.DTOs.Responses;

namespace AuthService.src.Application.Interfaces;

public interface IUserRepository
{
    Task<RefreshTokenResponse?> GetValidRefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<int> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<int> SaveRefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);
}