using BlogWebAPIApp.Context;
using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using static BlogWebAPIApp.Models.Enum;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BlogWebAPIApp.Controllers
{
    
    [ApiController]
    [Route("api")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _comments;
        private readonly BlogContext _db;


        public CommentsController(ICommentService comments, BlogContext db)
        {
            _comments = comments ?? throw new ArgumentNullException(nameof(comments));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }


      
        // Add comment or reply
        
        #region Adding Comment or reply
        [Authorize]
        [HttpPost("posts/{postId:guid}/comments")]
        public async Task<ActionResult<CommentDto>> Add(Guid postId, [FromBody] CreateCommentRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var c = await _comments.Add(postId, userId.Value, dto.Content, dto.ParentCommentId);
                return CreatedAtRoute("GetCommentById", new { id = c.Id }, c.ToDto());
                // New comments are either Approved or Pending depending on post settings
                //return CreatedAtAction(nameof(GetThreaded), new { id = c.Id }, c.ToDto());
            }
            catch (InvalidOperationException ex)
            {
                // e.g., "Comments disabled" or "Invalid parent comment"
                return BadRequest(new { message = ex.Message });
            }
            catch (BlogWebAPIApp.Exceptions.EntityNotFoundException)
            {
                return NotFound(new { message = "Post not found" });
            }
        }
        #endregion


        [AllowAnonymous]
        [HttpGet("comments/{id:guid}", Name = "GetCommentById")]
        public async Task<ActionResult<CommentDto>> GetById(Guid id)
        {
            var c = await _comments.GetById(id);
            if (c == null) return NotFound();
            return Ok(c.ToDto());
        }


        // Get approved threaded comments for a post (paged)

        #region GetComments with replies
        [AllowAnonymous]
        [HttpGet("posts/{postId:guid}/comments/threaded")]
        public async Task<ActionResult<List<object>>> GetThreaded(Guid postId, int page = 1, int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { message = "page and pageSize must be positive" });

            // top-level
            var parents = await _db.Comments
                .Include(c => c.Author)
                .Where(c => c.PostId == postId
                         && c.Status == CommentStatus.Approved
                         && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var parentIds = parents.Select(p => p.Id).ToList();

            // replies for these parents
            var replies = await _db.Comments
                .Include(c => c.Author)
                .Where(c => c.PostId == postId
                         && c.ParentCommentId != null
                         && parentIds.Contains(c.ParentCommentId.Value)
                         && c.Status == CommentStatus.Approved)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // group replies under parents
            var repliesLookup = replies.GroupBy(r => r.ParentCommentId!.Value)
                                       .ToDictionary(g => g.Key, g => g.Select(x => x.ToDto()).ToList());

            var result = parents.Select(p => new {
                parent = p.ToDto(repliesCount: repliesLookup.TryGetValue(p.Id, out var r) ? r.Count : 0),
                replies = repliesLookup.TryGetValue(p.Id, out var rr) ? rr : new List<CommentDto>()
            });

            return Ok(result);
        }
        #endregion

        
        // Update a comment
        //  - Author can update content
        //  - Post author can change status ("Pending"|"Approved"|"Removed")
        
        #region UpdateComment by ID
        [Authorize]
        [HttpPut("comments/{id:guid}")]
        public async Task<ActionResult<CommentDto>> Update(Guid id, [FromBody] UpdateCommentRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                // Service enforces permissions (author vs post author)
                await _comments.Update(id, userId.Value, dto.Content ?? string.Empty, dto.Status);
                var c = await _comments.GetById(id);
                if (c == null) return NotFound();
                return Ok(c.ToDto());
            }
            catch (BlogWebAPIApp.Exceptions.EntityNotFoundException)
            {
                return NotFound();
            }
            catch (BlogWebAPIApp.Exceptions.UnAuthorizedException )
            {
                return Forbid();
            }
            catch (InvalidOperationException )
            {
                return BadRequest(new { message = "Updating failed" });
            }
        }
        #endregion

        
        // Delete a comment
        //  - Author or Post author can delete
       
        #region Delete Comment
        [Authorize]
        [HttpDelete("comments/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                await _comments.Delete(id, userId.Value);
                return Ok(new { message = "Comment deleted successfully" });
            }
            catch (BlogWebAPIApp.Exceptions.EntityNotFoundException)
            {
                return NotFound();
            }
            catch (BlogWebAPIApp.Exceptions.UnAuthorizedException )
            {
                return Forbid();
            }
        }
        #endregion


        // ====================================
        // ✅ ADMIN: Get pending comments
        // ====================================
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetPending(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _comments.GetPendingComments(page, pageSize);

            return Ok(new
            {
                total,
                items = items.Select(c => c.ToDto())
            });
        }

        // ====================================
        // ✅ ADMIN: Approve a comment
        // ====================================
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id:guid}/approve")]
        public async Task<IActionResult> AdminApprove(Guid id)
        {
            await _comments.AdminApprove(id);
            return Ok(new { message = "Comment approved" });
        }

        // ====================================
        // ✅ ADMIN: Delete ANY comment
        // ====================================
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id:guid}")]
        public async Task<IActionResult> AdminDelete(Guid id)
        {
            await _comments.AdminDelete(id);
            return Ok(new { message = "Comment deleted by admin" });
        }


    }




}
