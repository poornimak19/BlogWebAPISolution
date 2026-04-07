using BlogWebAPIApp.Context;
using BlogWebAPIApp.Extensions;
using BlogWebAPIApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogWebAPIApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/premium")]
    public class PremiumController : ControllerBase
    {
        private readonly BlogContext _db;
        private const int FREE_PREMIUM_READS_PER_MONTH = 2;
        private const int PREVIEW_CHARS = 100;

        public PremiumController(BlogContext db) => _db = db;

        // POST /api/premium/subscribe  — simulate payment & activate subscription
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe()
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var user = await _db.Users.FindAsync(userId.Value);
            if (user is null) return Unauthorized();

            // Simulate payment success — extend by 30 days from now (or from current expiry)
            var from = (user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt > DateTime.UtcNow)
                ? user.PremiumExpiresAt.Value
                : DateTime.UtcNow;

            user.IsPremiumSubscriber = true;
            user.PremiumExpiresAt    = from.AddDays(30);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Premium activated!", expiresAt = user.PremiumExpiresAt });
        }

        // GET /api/premium/access/{postId}  — check if user can read this premium post fully
        [AllowAnonymous]
        [HttpGet("access/{postId:guid}")]
        public async Task<IActionResult> CheckAccess(Guid postId)
        {
            var post = await _db.Posts.FindAsync(postId);
            if (post is null) return NotFound();

            // Non-premium post — always full access
            if (!post.IsPremium)
                return Ok(new PremiumAccessDto(true, false, 0, PREVIEW_CHARS));

            var userId = User.GetUserId();

            // Not logged in — preview only
            if (userId is null)
                return Ok(new PremiumAccessDto(false, true, 0, PREVIEW_CHARS));

            var user = await _db.Users.FindAsync(userId.Value);
            if (user is null)
                return Ok(new PremiumAccessDto(false, true, 0, PREVIEW_CHARS));

            // Active premium subscriber — full access
            var isActivePremium = user.IsPremiumSubscriber &&
                                  (user.PremiumExpiresAt == null || user.PremiumExpiresAt > DateTime.UtcNow);
            if (isActivePremium)
                return Ok(new PremiumAccessDto(true, true, 0, PREVIEW_CHARS));

            // Author always has full access to their own post
            if (post.AuthorId == userId.Value)
                return Ok(new PremiumAccessDto(true, false, 0, PREVIEW_CHARS));

            // Count distinct premium posts read this calendar month
            var now        = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var readsThisMonth = await _db.PremiumReadLogs
                .Where(r => r.UserId == userId.Value && r.ReadAt >= monthStart)
                .Select(r => r.PostId)
                .Distinct()
                .CountAsync();

            // Already read this specific post this month — allow full access
            var alreadyRead = await _db.PremiumReadLogs
                .AnyAsync(r => r.UserId == userId.Value && r.PostId == postId && r.ReadAt >= monthStart);

            if (alreadyRead)
                return Ok(new PremiumAccessDto(true, false, readsThisMonth, PREVIEW_CHARS));

            // Still has free reads left
            if (readsThisMonth < FREE_PREMIUM_READS_PER_MONTH)
            {
                // Log this read
                _db.PremiumReadLogs.Add(new PremiumReadLog
                {
                    UserId = userId.Value,
                    PostId = postId,
                    ReadAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return Ok(new PremiumAccessDto(true, false, readsThisMonth + 1, PREVIEW_CHARS));
            }

            // Quota exhausted — preview only
            return Ok(new PremiumAccessDto(false, false, readsThisMonth, PREVIEW_CHARS));
        }
    }

    public record PremiumAccessDto(
        bool FullAccess,
        bool IsPremiumSubscriber,
        int ReadsThisMonth,
        int PreviewChars
    );
}
