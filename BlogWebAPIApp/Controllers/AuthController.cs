using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var (user, token) = await _auth.Register(dto.Email, dto.Username, dto.Password, dto.DisplayName,dto.Role);
                return Ok(new AuthResponseDto(token, user.Username, user.Role.ToString()));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        #endregion

        #region Login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var (user, token) = await _auth.Login(dto.EmailOrUsername, dto.Password);
                return Ok(new AuthResponseDto(token, user.Username, user.Role.ToString()));
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



    }

}

