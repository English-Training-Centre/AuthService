using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController (IUserServices userServices, ILogger<AuthController> logger) : ControllerBase, IAuthController
    {
        private readonly IUserServices _userServices = userServices;
        private readonly ILogger<AuthController> _logger = logger;

        [EnableRateLimiting("SignInPolicy")]
        [HttpPost("v1/sign-in")]  
        public async Task<IActionResult> SignIn([FromBody] AuthRequestDTO auth)
        {
            if (!ModelState.IsValid) return BadRequest(new AuthResponseDTO { IsSuccess = false, Message = "Invalid input." });

            try
            {
                var result = await _userServices.AuthUserAsync(auth);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Erro interno no servidor." });
            }
        }
    }
}