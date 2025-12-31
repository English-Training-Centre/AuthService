using AuthService.src.DTOs;

namespace AuthService.src.Interfaces
{
    public interface IUserServices
    {
        Task<AuthResponseDTO> AuthUserAsync(AuthRequestDTO user);
    }
}