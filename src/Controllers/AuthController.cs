using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.src.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]    
    public class AuthController (IAuthHandler authHandler, ILogger<AuthController> logger) : ControllerBase, IAuthController
    {
        private readonly IAuthHandler _authHandler = authHandler;
        private readonly ILogger<AuthController> _logger = logger;

        [EnableRateLimiting("LimitSignIn")]
        [HttpPost("sign-in")]  
        public async Task<IActionResult> SignIn([FromBody] AuthRequestDTO auth)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure("Invalid input.") );

            var result = await _authHandler.SignIn(auth);

            if (result is null || result.IsSuccess == false || result.AccessToken is null || result.RefreshToken is null) { return Unauthorized(ResponseDTO.Failure("Invalid credentials.")); }

            SetAccessTokenCookie(result.AccessToken);
            SetRefreshTokenCookie(result.RefreshToken); 

            return Ok(new AuthResponseDTO
            {
                IsSuccess = result.IsSuccess,
                UserId = result.UserId,
                Role = result.Role,
                AccessToken = result.AccessToken
            });
        }

        [EnableRateLimiting("LimitRefreshToken")]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["rfs_tk"];                
            if (string.IsNullOrEmpty(refreshToken)) return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Refresh token missing." });

            var tokenRecord = await _authHandler.GetValidRefreshToken(refreshToken);
            if (tokenRecord is null)
            {
                Response.Cookies.Delete("rfs_tk", GetSecureCookieOptions());
                return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Invalid or expired refresh token." });
            }

            var result = await _authHandler.RefreshToken(tokenRecord.UserId, refreshToken);

            if (result is null || result.IsSuccess == false || result.AccessToken is null || result.RefreshToken is null) { return Unauthorized(ResponseDTO.Failure("Not Found.")); }
            
            SetAccessTokenCookie(result.AccessToken);
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(true);
        }

        [Authorize]
        [HttpPost("sign-out")]
        public new async Task<IActionResult> SignOut()
        {
            try
            {
                var refreshToken = Request.Cookies["rfs_tk"];
                if (!string.IsNullOrWhiteSpace(refreshToken)) { await _authHandler.RevokeRefreshToken(refreshToken); }

                Response.Cookies.Delete("acc_tk", GetAccessTokenCookieOptions());
                Response.Cookies.Delete("rfs_tk", GetSecureCookieOptions());

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sign out for user.");
                return BadRequest(new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Unabel to sign out. Please try again."
                });
            }
        }

        [HttpGet("check-session")]
        public async Task<IActionResult> CheckSession()
        {
            try
            {
                var refreshToken = Request.Cookies["rfs_tk"];

                // Caso não tenha cookie de refresh token
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Ok(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "No active session."
                    });
                }

                // Verifica se o refresh token ainda é válido no banco
                var tokenRecord = await _authHandler.GetValidRefreshToken(refreshToken);

                if (tokenRecord == null)
                {
                    // Token inválido ou expirado → limpa cookies para evitar loops
                    Response.Cookies.Delete("acc_tk", GetAccessTokenCookieOptions());
                    Response.Cookies.Delete("rfs_tk", GetSecureCookieOptions());

                    return Ok(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Session expired or invalid."
                    });
                }

                var result = await _authHandler.CheckSession(tokenRecord.UserId);
                if (result is null || result.IsSuccess == false || result.AccessToken is null) { return Unauthorized(ResponseDTO.Failure("Not Found.")); }


                SetAccessTokenCookie(result.AccessToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session.");
                return StatusCode(500, new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Error verifying session."
                });
            }
        }

        private void SetAccessTokenCookie(string token)
        {
            Response.Cookies.Append("acc_tk", token, GetAccessTokenCookieOptions());
        }

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("rfs_tk", token, GetSecureCookieOptions());
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
                Expires = DateTimeOffset.UtcNow.AddMonths(2),
                Path = "/"
            };
        }
    }
}