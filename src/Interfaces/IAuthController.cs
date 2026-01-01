using AuthService.src.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.src.Interfaces
{
    public interface IAuthController
    {
        Task<IActionResult> SignIn([FromBody] AuthRequestDTO auth);
        Task<IActionResult> RefreshToken();
        Task<IActionResult> SignOut();
        Task<IActionResult> CheckSession();
    }
}