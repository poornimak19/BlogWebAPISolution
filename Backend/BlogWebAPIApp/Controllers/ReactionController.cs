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
    [Route("api")]
    public class ReactionsController : ControllerBase
    {
        private readonly IReactionService _reactions;

        public ReactionsController(IReactionService reactions)
        {
            _reactions = reactions;
        }

        // Toggle Post Like
        #region Post Like
        [Authorize]
        [HttpPost("posts/{postId:guid}/like")]
        public async Task<ActionResult<ReactionResponseDto>> TogglePostLike(Guid postId)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _reactions.TogglePostLike(postId, userId.Value);
            return Ok(result.ToDto());
        }
        #endregion


        // Toggle Comment Like
        #region Comment Like
        [Authorize]
        [HttpPost("comments/{commentId:guid}/like")]
        public async Task<ActionResult<ReactionResponseDto>> ToggleCommentLike(Guid commentId)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _reactions.ToggleCommentLike(commentId, userId.Value);
            return Ok(result.ToDto());
        }
        #endregion
    }

}
