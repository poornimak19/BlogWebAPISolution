namespace BlogWebAPIApp.Models
{
    /// <summary>
    /// Represents a single audit trail entry for any tracked action in the system.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The user who performed the action. Null for anonymous/system actions.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Action type: Create, Update, Delete, Login, Logout, etc.</summary>
        public string Action { get; set; } = default!;

        /// <summary>The entity type affected (e.g. "Post", "Comment", "User").</summary>
        public string EntityName { get; set; } = default!;

        /// <summary>The primary key of the affected entity as a string.</summary>
        public string EntityId { get; set; } = default!;

        /// <summary>JSON snapshot of the entity's values before the change. Null for Create.</summary>
        public string? OldValues { get; set; }

        /// <summary>JSON snapshot of the entity's values after the change. Null for Delete.</summary>
        public string? NewValues { get; set; }

        /// <summary>Client IP address extracted from the HTTP context.</summary>
        public string? IpAddress { get; set; }

        /// <summary>Client User-Agent header.</summary>
        public string? UserAgent { get; set; }

        /// <summary>UTC timestamp of when the action occurred.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Whether the action succeeded or failed.</summary>
        public string Status { get; set; } = AuditStatus.Success;

        /// <summary>Human-readable description of the action.</summary>
        public string? Description { get; set; }
    }

    public static class AuditStatus
    {
        public const string Success = "Success";
        public const string Failed  = "Failed";
    }

    public static class AuditActions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Login  = "Login";
        public const string Logout = "Logout";
        public const string Register = "Register";
        public const string ForgotPassword = "ForgotPassword";
        public const string ResetPassword  = "ResetPassword";
    }
}
