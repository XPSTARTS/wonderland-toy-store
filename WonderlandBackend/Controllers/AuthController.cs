using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WonderlandBackend.DTOs;
using WonderlandBackend.Services;
using System.Security.Claims;

namespace WonderlandBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _authService.Register(registerDto);

            if (user == null)
                return BadRequest(new { message = "User with this email already exists" });

            return Ok(new { message = "Registration successful", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.Login(loginDto);

            if (response == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var response = await _authService.RefreshToken(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            var result = await _authService.RevokeToken(request);
            if (!result)
                return BadRequest(new { message = "Invalid refresh token" });

            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.Logout(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("2fa/toggle")]
        [Authorize]
        public async Task<IActionResult> ToggleTwoFactor([FromBody] EnableTwoFactorDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _authService.ToggleTwoFactor(userId, dto.Enable);
            return Ok(result);
        }

        [HttpPost("2fa/send")]
        public async Task<IActionResult> SendTwoFactorCode([FromBody] SendTwoFactorDto dto)
        {
            var result = await _authService.SendTwoFactorCode(dto.Email);
            if (!result)
                return BadRequest(new { message = "Failed to send 2FA code" });
            return Ok(new { message = "2FA code sent to your email" });
        }

        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            var response = await _authService.VerifyTwoFactor(dto);
            if (response == null)
                return Unauthorized(new { message = "Invalid 2FA code" });
            return Ok(response);
        }
    }
}