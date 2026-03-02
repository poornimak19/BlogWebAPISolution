using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _posts;

        public PostsController(IPostService posts)
        {
            _posts = posts;
        }

        // Create a new post (Blogger/Admin)
        #region Create Posts
        [Authorize(Policy = "BloggerOnly")]
        [HttpPost]
        public async Task<ActionResult<PostDetailDto>> Create([FromBody] CreatePostRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var post = await _posts.Create(
                    authorId: userId.Value,
                    title: dto.Title,
                    slug: dto.Slug,
                    excerpt: dto.Excerpt,
                    contentHtml: dto.ContentHtml,
                    contentMarkdown: dto.ContentMarkdown,
                    visibility: dto.Visibility,
                    tagNames: dto.TagNames,
                    categoryNames: dto.CategoryNames,
                    allowedUserIds: dto.AllowedUserIds,
                    commentsEnabled: dto.CommentsEnabled,
                    autoApproveComments: dto.AutoApproveComments
                );

                return CreatedAtAction(nameof(GetBySlug), new { slug = post.Slug }, post.ToDetailDto());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        // Update a post (author only)
        #region Update post by ID
        [Authorize(Policy = "BloggerOnly")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PostDetailDto>> Update(Guid id, [FromBody] UpdatePostRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var post = await _posts.Update(
                    postId: id,
                    actorUserId: userId.Value,
                    title: dto.Title,
                    slug: dto.Slug,
                    excerpt: dto.Excerpt,
                    contentHtml: dto.ContentHtml,
                    contentMarkdown: dto.ContentMarkdown,
                    visibility: dto.Visibility,
                    tagNames: dto.TagNames,
                    categoryNames: dto.CategoryNames,
                    allowedUserIds: dto.AllowedUserIds,
                    commentsEnabled: dto.CommentsEnabled,
                    autoApproveComments: dto.AutoApproveComments,
                    status: dto.Status
                );

                return Ok(post.ToDetailDto());
            }
            catch (Exceptions.EntityNotFoundException)
            {
                return NotFound();
            }
            catch (Exceptions.UnAuthorizedException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        // Publish a post (author only)
        #region Post Publishing
        [Authorize(Policy = "BloggerOnly")]
        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                await _posts.Publish(id, userId.Value);
                return Ok(new { message = "Post published successfully" });
            }
            catch (Exceptions.EntityNotFoundException)
            {
                return NotFound();
            }
            catch (Exceptions.UnAuthorizedException ex)
            {
                return Forbid(ex.Message);
            }

        }
        #endregion
        
        // Delete a post (author only)        
        #region Post Deleting
        [Authorize(Policy = "BloggerOnly")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            try
            {
                await _posts.Delete(id, userId.Value);
                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exceptions.EntityNotFoundException)
            {
                return NotFound();
            }
            catch (Exceptions.UnAuthorizedException)
            {
                return Forbid();
            }
        }
        #endregion

       
        // Get published posts (public)
        // Filters: q, tagSlug, categorySlug; paging       
        #region Get all Post "PUBLIC"
        [AllowAnonymous]
        [HttpGet("published")]
        public async Task<ActionResult<PagedResponseDto<PostSummaryDto>>> GetPublished(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? q = null,
            [FromQuery(Name = "tag")] string? tagSlug = null,
            [FromQuery(Name = "category")] string? categorySlug = null)
        {
            if (page <= 0 || pageSize <= 0) return BadRequest(new { message = "page and pageSize must be positive" });

            var (items, total) = await _posts.GetPublished(page, pageSize, q, tagSlug, categorySlug);
            var payload = new PagedResponseDto<PostSummaryDto>(
                items.Select(p => p.ToSummaryDto()).ToList(),
                total, page, pageSize);

            return Ok(payload);
        }
        #endregion

        
        // Get a single post by slug
        // Enforces visibility rules:
        //   - Draft/Archived: only author
        //   - Private: only author
        //   - Restricted: author or allowed audience
        //   - Public: anyone (if Published)
        #region GetPost by slug
        [AllowAnonymous]
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<PostDetailDto>> GetBySlug(string slug)
        {
            var post = await _posts.GetBySlug(slug);
            if (post == null) return NotFound();

            var currentUserId = User.GetUserId();
            if (!CanView(post, currentUserId))
                return NotFound(); // avoid leaking existence

            return Ok(post.ToDetailDto());
        }
        #endregion

        
        // Get current user's posts (any status/visibility)        
        #region GetPost by token
        [Authorize]
        [HttpGet("mine")]
        public async Task<ActionResult<PagedResponseDto<PostSummaryDto>>> GetMine(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var (items, total) = await _posts.GetByAuthor(userId.Value, page, pageSize);
            var payload = new PagedResponseDto<PostSummaryDto>(
                items.Select(p => p.ToSummaryDto()).ToList(),
                total, page, pageSize);

            return Ok(payload);
        }
        #endregion

        
        // Local visibility guard       
        private static bool CanView(Post post, Guid? currentUserId)
        {
            var isAuthor = currentUserId.HasValue && post.AuthorId == currentUserId.Value;

            // Draft/Archived hidden except to author
            if (post.Status != PostStatus.Published)
                return isAuthor;

            // Published flow
            return post.Visibility switch
            {
                Visibility.Public => true,
                Visibility.Private => isAuthor,
                Visibility.Restricted => isAuthor || (currentUserId.HasValue && post.Audience.Any(a => a.UserId == currentUserId.Value)),
                _ => false
            };
        }
    }

}
