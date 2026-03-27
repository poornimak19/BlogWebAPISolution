using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IUserService _users;

        public AuthController(IAuthService auth, IUserService users)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _users = users ?? throw new ArgumentNullException(nameof(users));
        }

        #region Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (dto.Role == UserRole.Admin && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var (user, token) = await _auth.Register(
                    dto.Email,
                    dto.Username,
                    dto.Password,
                    dto.DisplayName,
                    dto.Role
                );

                // Return ONLY the token
                return Ok(new { token });
                // If you prefer a bare string:
                // return Ok(token);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        #endregion

        #region Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var (user, token) = await _auth.Login(dto.EmailOrUsername, dto.Password);

                // Return ONLY the token
                return Ok(new { token });
                // Or:
                // return Ok(token);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
        #endregion

        #region User Deets
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<MeResponseDto>> Me()
        {
            // Use the safe helper which already checks IsAuthenticated and parses Guid
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            // Try to get role from claims; fall back to user's stored role if missing
            var role = User.FindFirstValue(ClaimTypes.Role);

            var user = await _users.GetById(userId.Value);
            if (user is null) return Unauthorized(); // stale token or user removed

            return Ok(new MeResponseDto(
                Id: user.Id,
                Username: user.Username,
                DisplayName: user.DisplayName,
                Email: user.Email,
                Role: role ?? user.Role.ToString()
            ));
        }
        #endregion


        #region Forgot / Reset Password
        // Step-1: user submits email → we issue a short-lived reset JWT
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // In production, return only a neutral message.
            var token = await _auth.ForgotPassword(dto.Email);
            return Ok(new
            {
                message = "If your email exists, you will receive reset instructions.",
#if DEBUG
                // For local testing only
                token
#endif
            });
        }

        // Step-2: user submits token + newPassword → update password if token valid
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            await _auth.ResetPassword(dto.Token, dto.NewPassword);
            return Ok(new { message = "Password has been reset successfully." });
        }
        #endregion


    }
}