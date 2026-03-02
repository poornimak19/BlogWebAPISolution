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
    public class FollowsController : ControllerBase
    {
        private readonly IFollowService _follows;

        public FollowsController(IFollowService follows)
        {
            _follows = follows;
        }

        // Toggle follow/unfollow
        
        #region Follow 
        [Authorize]
        [HttpPost("{id:guid}/follow")]
        public async Task<ActionResult<FollowToggleResponseDto>> ToggleFollow(Guid id)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId is null) return Unauthorized();

            try
            {
                var result = await _follows.ToggleFollow(currentUserId.Value, id);
                return Ok(result.ToToggleDto());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        
        // Get follow counts
        
        #region Follows count
        [AllowAnonymous]
        [HttpGet("{id:guid}/follows/counts")]
        public async Task<ActionResult<FollowCountsDto>> GetCounts(Guid id)
        {
            var result = await _follows.GetCounts(id);
            return Ok(result.ToCountsDto());
        }
        #endregion
    }

}
