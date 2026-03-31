namespace BlogWebAPIApp.Models.DTOs
{
    public record AuditLogDto(
        Guid     Id,
        string?  Username,
        string?  UserRole,
        string   Action,
        string   EntityName,
        string   EntityId,
        string?  OldValues,
        string?  NewValues,
        string?  UserAgent,
        DateTime Timestamp,
        string   Status,
        string?  Description
    );

    public class AuditLogFilterDto
    {
        public Guid?     UserId     { get; set; }
        public string?   Action     { get; set; }
        public string?   EntityName { get; set; }
        public DateTime? From       { get; set; }
        public DateTime? To         { get; set; }
        public string?   Status     { get; set; }
        public int       Page       { get; set; } = 1;
        public int       PageSize   { get; set; } = 20;
    }
}
