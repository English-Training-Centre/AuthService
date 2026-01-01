using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController (IAuthRepository authRepository, IUserServices userServices, ILogger<AuthController> logger) : ControllerBase, IAuthController
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly IUserServices _userServices = userServices;
        private readonly ILogger<AuthController> _logger = logger;

        [EnableRateLimiting("SignInPolicy")]
        [HttpPost("v1/sign-in")]  
        public async Task<IActionResult> SignIn([FromBody] AuthRequestDTO auth)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure("Invalid input.") );

            try
            {
                var result = await _userServices.AuthUserAsync(auth);

                if (result is null || result.IsSuccess == false) { return Unauthorized(ResponseDTO.Failure("Invalid credentials.")); }

                var user = new AuthUserDTO
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Role = result.Role
                };
                
                var accessToken = _authRepository.GenerateAccessToken(user);
                var refreshToken = await _authRepository.GenerateRefreshToken(user.Id);

                SetAccessTokenCookie(accessToken);
                SetRefreshTokenCookie(refreshToken); 

                return Ok(new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = result.Message,
                    UserId = user.Id,
                    Role = user.Role,
                    AccessToken = accessToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error 503");
                return StatusCode(503, new { message = ex.Message });
            }
            catch (Exception)
            {
                _logger.LogError("Error 500");
                return StatusCode(500, new { message = "Erro interno no servidor." });
            }
        }

        private void SetAccessTokenCookie(string token)
        {
            Response.Cookies.Append("accessToken", token, GetAccessTokenCookieOptions());
        }

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, GetSecureCookieOptions());
        }

        private static CookieOptions GetAccessTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,    // Lax is safer than None
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                Path = "/"                      // Sent on all requests
            };
        }

        private static CookieOptions GetSecureCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };
        }
    }
}