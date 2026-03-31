using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService     _auth;
        private readonly IUserService     _users;
        private readonly IAuditLogService _auditLogs;

        public AuthController(IAuthService auth, IUserService users, IAuditLogService auditLogs)
        {
            _auth      = auth      ?? throw new ArgumentNullException(nameof(auth));
            _users     = users     ?? throw new ArgumentNullException(nameof(users));
            _auditLogs = auditLogs ?? throw new ArgumentNullException(nameof(auditLogs));
        }

        #region Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (dto.Role == UserRole.Admin && !User.IsInRole("Admin"))
                return Forbid();

            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var (user, token) = await _auth.Register(dto.Email, dto.Username, dto.Password, dto.DisplayName, dto.Role);

                await _auditLogs.LogAsync(
                    AuditActions.Register, "User", user.Id.ToString(),
                    userId: user.Id,
                    description: $"New user '{user.Username}' registered with role {user.Role}");

                return Ok(new { token });
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogs.LogAsync(
                    AuditActions.Register, "User", dto.Email,
                    description: $"Registration failed for '{dto.Email}': {ex.Message}",
                    status: AuditStatus.Failed);

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

                await _auditLogs.LogAsync(
                    AuditActions.Login, "User", user.Id.ToString(),
                    userId: user.Id,
                    description: $"User '{user.Username}' logged in");

                return Ok(new { token });
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogs.LogAsync(
                    AuditActions.Login, "User", dto.EmailOrUsername,
                    description: $"Login failed for '{dto.EmailOrUsername}': {ex.Message}",
                    status: AuditStatus.Failed);

                return Unauthorized(new { message = ex.Message });
            }
        }
        #endregion

        #region User Deets
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<MeResponseDto>> Me()
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var user = await _users.GetById(userId.Value);
            if (user is null) return Unauthorized();

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
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var token = await _auth.ForgotPassword(dto.Email);

            await _auditLogs.LogAsync(
                AuditActions.ForgotPassword, "User", dto.Email,
                description: $"Password reset requested for '{dto.Email}'");

            return Ok(new
            {
                message = "If your email exists, you will receive reset instructions.",
#if DEBUG
                token
#endif
            });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                await _auth.ResetPassword(dto.Token, dto.NewPassword);

                await _auditLogs.LogAsync(
                    AuditActions.ResetPassword, "User", "unknown",
                    description: "Password reset completed successfully");

                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                await _auditLogs.LogAsync(
                    AuditActions.ResetPassword, "User", "unknown",
                    description: $"Password reset failed: {ex.Message}",
                    status: AuditStatus.Failed);

                throw; // let ErrorLoggingMiddleware handle it
            }
        }
        #endregion
    }
}