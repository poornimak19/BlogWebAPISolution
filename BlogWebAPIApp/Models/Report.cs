using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models
{
    public class Report
    {

        public Guid Id { get; set; }

        public ReportTargetType TargetType { get; set; } // Post or Comment
        public Guid TargetId { get; set; }

        public Guid ReporterId { get; set; }
        public User Reporter { get; set; } = default!;

        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; } = ReportStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public Guid? ResolvedById { get; set; }
        public User? ResolvedBy { get; set; }
        public string? ResolutionNote { get; set; }

    }
}
