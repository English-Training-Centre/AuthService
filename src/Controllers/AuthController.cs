using AuthService.src.DTOs;
using AuthService.src.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        [EnableRateLimiting("LimitSignIn")]
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
                    IsSuccess = result.IsSuccess,
                    UserId = result.UserId,
                    Role = result.Role,
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

        [EnableRateLimiting("LimitRefreshToken")]
        [HttpPost("v1/refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];                
                if (string.IsNullOrEmpty(refreshToken)) return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Refresh token missing." });

                var tokenRecord = await _authRepository.GetValidRefreshToken(refreshToken);
                if (tokenRecord is null)
                {
                    Response.Cookies.Delete("refreshToken", GetSecureCookieOptions());
                    return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Invalid or expired refresh token." });
                }                

                var result = await _userServices.GetUserByIdAsync(tokenRecord.UserId);
                if (result is null || result.IsSuccess == false) { return Unauthorized(ResponseDTO.Failure("Not Found.")); }

                var user = new AuthUserDTO
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Role = result.Role
                };

                var newAccessToken = _authRepository.GenerateAccessToken(user);
                var newRefreshToken = await _authRepository.GenerateRefreshToken(user.Id);

                await _authRepository.RevokeRefreshToken(refreshToken);
                SetAccessTokenCookie(newAccessToken);
                SetRefreshTokenCookie(newRefreshToken);

                return Ok(new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session expired. Please sign in again.");
                return BadRequest(new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Session expired. Please sign in again."
                });
            }
        }

        [Authorize]
        [HttpPost("v1/sign-out")]
        public new async Task<IActionResult> SignOut()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (!string.IsNullOrWhiteSpace(refreshToken)) { await _authRepository.RevokeRefreshToken(refreshToken); }

                Response.Cookies.Delete("accessToken", GetAccessTokenCookieOptions());
                Response.Cookies.Delete("refreshToken", GetSecureCookieOptions());

                return Ok(new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "Signed out successfully."
                });
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

        [HttpGet("v1/check-session")]
        public async Task<IActionResult> CheckSession()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];

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
                var tokenRecord = await _authRepository.GetValidRefreshToken(refreshToken);

                if (tokenRecord == null)
                {
                    // Token inválido ou expirado → limpa cookies para evitar loops
                    Response.Cookies.Delete("accessToken", GetAccessTokenCookieOptions());
                    Response.Cookies.Delete("refreshToken", GetSecureCookieOptions());

                    return Ok(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Session expired or invalid."
                    });
                }

                // Token válido → usuário está logado
                // Opcional: já gera um novo access token curto aqui se quiser (boa prática)
                var userResult = await _userServices.GetUserByIdAsync(tokenRecord.UserId);
                if (userResult == null || !userResult.IsSuccess)
                {
                    return Ok(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = "User not found."
                    });
                }

                var user = new AuthUserDTO
                {
                    Id = userResult.UserId,
                    Username = userResult.Username,
                    Role = userResult.Role
                };

                var newAccessToken = _authRepository.GenerateAccessToken(user);

                // Atualiza o access token no cookie (opcional, mas recomendado)
                SetAccessTokenCookie(newAccessToken);

                return Ok(new AuthResponseDTO
                {
                    IsSuccess = userResult.IsSuccess,
                    UserId = userResult.UserId,
                    Role = userResult.Role,
                    AccessToken = newAccessToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                });
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
                Expires = DateTimeOffset.UtcNow.AddMonths(2),
                Path = "/"
            };
        }
    }
}