using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Interfaces
{
    public interface IAuditLogService
    {
        /// <summary>Manually record a non-DB action (Login, Logout, etc.).</summary>
        Task LogAsync(
            string  action,
            string  entityName,
            string  entityId,
            Guid?   userId      = null,
            string? description = null,
            string? status      = null,
            string? oldValues   = null,
            string? newValues   = null);

        /// <summary>Paginated, filtered list of audit logs.</summary>
        Task<(IReadOnlyList<AuditLogDto> items, int total)> GetLogsAsync(AuditLogFilterDto filter);
    }
}
