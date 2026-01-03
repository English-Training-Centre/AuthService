using AuthService.src.DTOs;

namespace AuthService.src.Interfaces
{
    public interface IAuthRepository
    {        
        Task<RefreshTokenDTO?> GetValidRefreshToken(string token); 
        Task RevokeRefreshToken(string token);
        Task SaveRefreshToken(RefreshTokenDTO token);
    }
}