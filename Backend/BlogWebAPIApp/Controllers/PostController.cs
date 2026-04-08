using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _posts;
        private readonly IRepository<Guid, Report> _reports;
        private readonly IAuditLogService _auditLogs;

        public PostsController(IPostService posts, IRepository<Guid, Report> reports, IAuditLogService auditLogs)
        {
            _posts     = posts;
            _reports   = reports;
            _auditLogs = auditLogs;
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
                    commentsEnabled: dto.CommentsEnabled,
                    autoApproveComments: dto.AutoApproveComments,
                    coverImageUrl: dto.CoverImageUrl,
                    audioUrl: dto.AudioUrl,
                    videoUrl: dto.VideoUrl,
                    isPremium: dto.IsPremium
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
                    commentsEnabled: dto.CommentsEnabled,
                    autoApproveComments: dto.AutoApproveComments,
                    status: dto.Status,
                    coverImageUrl: dto.CoverImageUrl,
                    audioUrl: dto.AudioUrl,
                    videoUrl: dto.VideoUrl,
                    isPremium: dto.IsPremium
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
            catch (InvalidOperationException )
            {
                return BadRequest(new { message = "Update failed" });
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
            [FromQuery(Name = "category")] string? categorySlug = null,
            [FromQuery] Guid? currentUserId = null
        )
        {
            var (items, total) = await _posts.GetPublished(
                page, pageSize, q, tagSlug, categorySlug, currentUserId
            );

            var payload = new PagedResponseDto<PostSummaryDto>(
                items.Select(p => p.ToSummaryDto()).ToList(),
                total, page, pageSize
            );

            return Ok(payload);
        }

        #endregion


        // Get a single post by slug
        // Enforces visibility rules:
        //   - Draft/Archived: only author
        //   - Private: only followers
       
        //   - Public: anyone (if Published)
        #region GetPost by slug
        [AllowAnonymous]
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<PostDetailDto>> GetBySlug(string slug)
        {
            var post = await _posts.GetBySlug(slug);
            if (post == null) return NotFound(new { message = "Post Not Found.Enter the correct Tag name " });

            var currentUserId = User.GetUserId();
            var isAdmin = User.IsInRole("Admin");
            if (!CanView(post, currentUserId, isAdmin))
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



        // ---- VISIBILITY CHECK ----
        private bool CanView(Post post, Guid? viewerId, bool isAdmin = false)
        {
            if (isAdmin) return true;

            var isAuthor = viewerId.HasValue && viewerId.Value == post.AuthorId;

            // Draft / archived → only author
            if (post.Status != PostStatus.Published)
                return isAuthor;

            return post.Visibility switch
            {
                Visibility.Public => true,

                Visibility.Private =>
                    isAuthor ||
                    (
                        viewerId.HasValue &&
                        post.Author.Followers.Any(f => f.FollowerId == viewerId.Value)
                    ),

                _ => false
            };
        }


        
        // ✅ ADMIN: Get all pending posts
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<ActionResult<PagedResponseDto<PostSummaryDto>>> GetPendingPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _posts.GetPendingPosts(page, pageSize);

            var payload = new PagedResponseDto<PostSummaryDto>(
                items.Select(p => p.ToSummaryDto()).ToList(),
                total, page, pageSize
            );

            return Ok(payload);
        }

        // ✅ ADMIN: Approve post
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<IActionResult> AdminApprove(Guid id)
        {
            await _posts.ApprovePost(id);
            return Ok(new { message = "Post approved" });
        }

        // ✅ ADMIN: Reject post
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reject")]
        public async Task<IActionResult> AdminReject(Guid id)
        {
            await _posts.RejectPost(id);
            return Ok(new { message = "Post rejected" });
        }

        // ✅ ADMIN: Delete any post
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}/admin-delete")]
        public async Task<IActionResult> AdminDelete(Guid id)
        {
            await _posts.AdminDelete(id);
            return Ok(new { message = "Post deleted by admin" });
        }

        // ✅ ADMIN: Get post stats
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var (total, published, draft, pending) = await _posts.GetPostStats();
            return Ok(new { total, published, draft, pending });
        }

        // ✅ ADMIN: Get ALL posts (all statuses/visibility)
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<ActionResult<PagedResponseDto<PostSummaryDto>>> GetAllPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null,
            [FromQuery] string? visibility = null)
        {
            var (items, total) = await _posts.GetAllPosts(page, pageSize, q, visibility);

            // Batch-fetch report counts for this page
            var postIds = items.Select(p => p.Id).ToList();
            var reportCounts = await _reports.GetQueryable()
                .Where(r => r.TargetType == ReportTargetType.Post && postIds.Contains(r.TargetId) && r.Status == ReportStatus.Open)
                .GroupBy(r => r.TargetId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            return Ok(new PagedResponseDto<PostSummaryDto>(
                items.Select(p => p.ToSummaryDto(reportCounts.GetValueOrDefault(p.Id, 0))).ToList(),
                total, page, pageSize));
        }

        // ANY logged-in user: Report a post
        [Authorize]
        [HttpPost("{id:guid}/report")]
        public async Task<IActionResult> ReportPost(Guid id, [FromBody] ReportPostRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            // Prevent duplicate open reports from same user
            var already = await _reports.GetQueryable().AnyAsync(r =>
                r.TargetType == ReportTargetType.Post &&
                r.TargetId   == id &&
                r.ReporterId == userId.Value &&
                r.Status     == ReportStatus.Open);

            if (already)
                return Conflict(new { message = "You have already reported this post." });

            await _reports.Add(new Report
            {
                TargetType = ReportTargetType.Post,
                TargetId   = id,
                ReporterId = userId.Value,
                Reason     = dto.Reason.Trim(),
                Status     = ReportStatus.Open,
                CreatedAt  = DateTime.UtcNow
            });

            await _auditLogs.LogAsync(
                AuditActions.Report, "Report", id.ToString(),
                userId: userId.Value,
                description: $"User reported post '{id}': {dto.Reason.Trim()}");

            return Ok(new { message = "Report submitted. Thank you." });
        }

        // PUBLIC: Get report count for a post (admin use)
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}/report-count")]
        public async Task<IActionResult> GetReportCount(Guid id)
        {
            var count = await _reports.GetQueryable()
                .CountAsync(r => r.TargetType == ReportTargetType.Post && r.TargetId == id);
            return Ok(new { count });
        }

        // ADMIN: Get all reports for a post
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}/reports")]
        public async Task<IActionResult> GetPostReports(Guid id)
        {
            var reports = await _reports.GetQueryable()
                .Where(r => r.TargetType == ReportTargetType.Post && r.TargetId == id)
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedBy)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Reason,
                    r.Status,
                    r.CreatedAt,
                    r.ResolvedAt,
                    r.ResolutionNote,
                    Reporter   = new { r.Reporter.Username, r.Reporter.DisplayName },
                    ResolvedBy = r.ResolvedBy == null ? null : new { r.ResolvedBy.Username, r.ResolvedBy.DisplayName }
                })
                .ToListAsync();

            return Ok(reports);
        }

        // ADMIN: Resolve a report
        [Authorize(Roles = "Admin")]
        [HttpPut("{postId:guid}/reports/{reportId:guid}/resolve")]
        public async Task<IActionResult> ResolveReport(Guid postId, Guid reportId, [FromBody] ResolveReportDto dto)
        {
            var report = await _reports.GetQueryable()
                .FirstOrDefaultAsync(r => r.Id == reportId && r.TargetId == postId);
            if (report is null) return NotFound();

            var adminId = User.GetUserId();
            report.Status         = ReportStatus.Resolved;
            report.ResolvedAt     = DateTime.UtcNow;
            report.ResolvedById   = adminId;
            report.ResolutionNote = dto.Note?.Trim();

            await _reports.SaveChangesAsync();

            await _auditLogs.LogAsync(
                AuditActions.Resolve, "Report", reportId.ToString(),
                userId: adminId,
                description: $"Admin resolved report '{reportId}' on post '{postId}'" + (dto.Note != null ? $": {dto.Note}" : ""));

            return Ok(new { message = "Report resolved." });
        }

        // ADMIN: Dismiss a report
        [Authorize(Roles = "Admin")]
        [HttpPut("{postId:guid}/reports/{reportId:guid}/dismiss")]
        public async Task<IActionResult> DismissReport(Guid postId, Guid reportId)
        {
            var report = await _reports.GetQueryable()
                .FirstOrDefaultAsync(r => r.Id == reportId && r.TargetId == postId);
            if (report is null) return NotFound();

            var adminId = User.GetUserId();
            report.Status       = ReportStatus.Dismissed;
            report.ResolvedAt   = DateTime.UtcNow;
            report.ResolvedById = adminId;

            await _reports.SaveChangesAsync();

            await _auditLogs.LogAsync(
                AuditActions.Dismiss, "Report", reportId.ToString(),
                userId: adminId,
                description: $"Admin dismissed report '{reportId}' on post '{postId}'");

            return Ok(new { message = "Report dismissed." });
        }

        // Upload audio or video file — returns a relative URL
        [Authorize(Policy = "BloggerOnly")]
        [HttpPost("upload-media")]
        [RequestSizeLimit(200 * 1024 * 1024)] // 200 MB
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var allowedTypes = new[]
            {
                "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/webm",
                "video/mp4", "video/webm", "video/ogg", "video/quicktime"
            };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { message = "Only audio and video files are allowed." });

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "media");
            Directory.CreateDirectory(folder);

            var ext      = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return Ok(new { url = $"/uploads/media/{fileName}" });
        }

    }

}
