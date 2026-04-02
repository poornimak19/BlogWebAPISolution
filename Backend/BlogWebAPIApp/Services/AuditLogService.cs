using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogWebAPIApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<Guid, AuditLog>  _db;
        private readonly IRepository<Guid, User> _user;
        private readonly IHttpContextAccessor _http;

        //public AuditLogService(BlogContext db, IHttpContextAccessor http)
        public AuditLogService(IRepository<Guid, AuditLog> db, IRepository<Guid, User> user,IHttpContextAccessor http)
        {
            _db   = db;
            _user = user;
            _http = http;
        }

        /// <inheritdoc/>
        public async Task LogAsync(
            string  action,
            string  entityName,
            string  entityId,
            Guid?   userId      = null,
            string? description = null,
            string? status      = null,
            string? oldValues   = null,
            string? newValues   = null)
        {
            var ctx = _http.HttpContext;

            var log = new AuditLog
            {
                Action      = action,
                EntityName  = entityName,
                EntityId    = entityId,
                UserId      = userId ?? ResolveUserId(ctx),
                Description = description,
                Status      = status ?? AuditStatus.Success,
                OldValues   = oldValues,
                NewValues   = newValues,
                IpAddress   = ResolveIpAddress(ctx),
                UserAgent   = ctx?.Request.Headers["User-Agent"].ToString(),
                Timestamp   = DateTime.UtcNow
            };

            // Use base.SaveChangesAsync to avoid triggering the audit override again
            var result = await _db.Add(log);
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<AuditLogDto> items, int total)> GetLogsAsync(AuditLogFilterDto filter)
        {
            var logQuery = _db.GetQueryable().AsNoTracking().AsQueryable();

            if (filter.UserId.HasValue)
                logQuery = logQuery.Where(l => l.UserId == filter.UserId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Action))
                logQuery = logQuery.Where(l => l.Action == filter.Action);
            if (!string.IsNullOrWhiteSpace(filter.EntityName))
                logQuery = logQuery.Where(l => l.EntityName == filter.EntityName);
            if (!string.IsNullOrWhiteSpace(filter.Status))
                logQuery = logQuery.Where(l => l.Status == filter.Status);
            if (filter.From.HasValue)
                logQuery = logQuery.Where(l => l.Timestamp >= filter.From.Value);
            if (filter.To.HasValue)
                logQuery = logQuery.Where(l => l.Timestamp <= filter.To.Value);

            var total = await logQuery.CountAsync();

            // Fetch the page of logs
            var logs = await logQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Collect distinct non-null user IDs from this page
            var userIds = logs
                .Where(l => l.UserId.HasValue)
                .Select(l => l.UserId!.Value)
                .Distinct()
                .ToList();

            // Single query to fetch only the users we need
            var userMap = userIds.Count > 0
                ? await _user.GetQueryable().AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Username, Role = u.Role.ToString() })
                    .ToListAsync()
                : [];

            var users = userMap.ToDictionary(u => u.Id);

            var rows = logs.Select(l =>
            {
                var user = l.UserId.HasValue && users.TryGetValue(l.UserId.Value, out var u) ? u : null;
                return new AuditLogDto(
                    l.Id,
                    user?.Username,
                    user?.Role,
                    l.Action,
                    l.EntityName,
                    l.EntityId,
                    l.OldValues,
                    l.NewValues,
                    l.UserAgent,
                    l.Timestamp,
                    l.Status,
                    l.Description
                );
            }).ToList();

            return (rows, total);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Guid? ResolveUserId(HttpContext? ctx)
        {
            var sub = ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? ctx?.User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }

        private static string? ResolveIpAddress(HttpContext? ctx)
        {
            if (ctx is null) return null;
            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : ctx.Connection.RemoteIpAddress?.ToString();
        }
    }
}
