using AuthService.src.DTOs;

namespace AuthService.src.Interfaces
{
    public interface IAuthRepository
    {
        string GenerateAccessToken(AuthUserDTO user);
        Task<string> GenerateRefreshToken(Guid userId);    
    }
}