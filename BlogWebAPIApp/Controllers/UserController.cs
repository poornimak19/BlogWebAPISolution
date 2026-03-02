using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogWebAPIApp.Controllers
{


    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;

        public UsersController(IUserService users)
        {
            _users = users;
        }

        // PUBLIC: Get user by username
        #region Get users
        [AllowAnonymous]
        [HttpGet("{username}")]
        public async Task<ActionResult<UserProfileDto>> GetByUsername(string username)
        {
            var user = await _users.GetByUsername(username);
            if (user == null) return NotFound();

            var (followers, following) = await _users.GetFollowCounts(user.Id);
            return Ok(user.ToProfileDto(followers, following));
        }
        #endregion

        // AUTH: Get your own profile        
        #region Get profile
        [Authorize]
        [HttpGet("me/profile")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var user = await _users.GetById(userId.Value);
            if (user == null) return NotFound();

            var (followers, following) = await _users.GetFollowCounts(user.Id);
            return Ok(user.ToProfileDto(followers, following));
        }
        #endregion

        // AUTH: Update your own profile        
        #region Update profile
        [Authorize]
        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            await _users.UpdateProfile(userId.Value, dto.DisplayName, dto.Bio, dto.AvatarUrl);

            // Fetch updated user
            var updatedUser = await _users.GetById(userId.Value);
            if (updatedUser == null) return NotFound();

            // Get follower/following counts
            var (followers, following) = await _users.GetFollowCounts(userId.Value);

            // Return updated DTO
            return Ok(updatedUser.ToProfileDto(followers, following));

        }
        #endregion
    }

}
