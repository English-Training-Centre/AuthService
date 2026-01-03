using AuthService.src.DTOs;

namespace AuthService.src.Interfaces
{
    public interface IAuthHandler
    {
        Task<UserResponseDTO> SignIn(AuthRequestDTO auth);
        Task<UserRefreshTokenDTO> RefreshToken(Guid userId, string refreshToken);
        Task<AuthResponseDTO> CheckSession(Guid userId);
        string GenerateAccessToken(AuthUserDTO user);
        Task<string> GenerateRefreshToken(Guid userId);  
        Task<RefreshTokenDTO?> GetValidRefreshToken(string token);  
        Task RevokeRefreshToken(string token);
    }
}