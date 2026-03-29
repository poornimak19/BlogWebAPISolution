using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models.DTOs
{


    public class CreateCommentRequestDto
    {
        /// <summary>Required comment content (max 8000 enforced by model)</summary>
        [Required, MaxLength(8000)]
        public string Content { get; set; } = default!;

        /// <summary>Optional parent comment (for replies)</summary>
        public Guid? ParentCommentId { get; set; }
    }

    public class UpdateCommentRequestDto
    {
        /// <summary>Optional content update. Only the author can update content.</summary>
        [MaxLength(8000)]
        public string? Content { get; set; }

        /// <summary>
        /// Optional status update. Only the post author can change status.
        /// One of: "Pending" | "Approved" | "Removed"
        /// </summary>
        public string? Status { get; set; }
    }

    public record CommentAuthorDto(
        Guid Id,
        string Username,
        string? DisplayName,
        string? AvatarUrl
    );

    public record CommentDto(
        Guid Id,
        Guid PostId,
        Guid? ParentCommentId,
        CommentAuthorDto? Author,
        string Content,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int RepliesCount,
        string? PostTitle
    );
}

