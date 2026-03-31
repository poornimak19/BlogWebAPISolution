using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogWebAPIApp.Controllers
{
    [Route("api/auditlogs")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogs;

        public AuditLogController(IAuditLogService auditLogs) => _auditLogs = auditLogs;

        /// <summary>
        /// GET /api/auditlogs
        /// Returns a paginated, filtered list of audit log entries.
        /// Accessible by Admin only.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] AuditLogFilterDto filter)
        {
            var (items, total) = await _auditLogs.GetLogsAsync(filter);
            return Ok(new PagedResponseDto<AuditLogDto>(items, total, filter.Page, filter.PageSize));
        }
    }
}
