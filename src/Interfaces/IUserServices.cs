using AuthService.src.DTOs;

namespace AuthService.src.Interfaces
{
    public interface IUserServices
    {
        Task<UserServiceAuthDTO> AuthUserAsync(AuthRequestDTO user);
    }
}