using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models.DTOs
{

    public class CreatePostRequestDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        [MaxLength(220)]
        public string? Slug { get; set; }

        [MaxLength(400)]
        public string? Excerpt { get; set; }

        [Required]
        public string ContentHtml { get; set; } = default!;

        public string? ContentMarkdown { get; set; }

        /// <summary> "Public" | "Private" | "Restricted" </summary>
        [Required]
        public string Visibility { get; set; } = "Public";

        public IEnumerable<string>? TagNames { get; set; }
        public IEnumerable<string>? CategoryNames { get; set; }
        public IEnumerable<Guid>? AllowedUserIds { get; set; }

        public bool? CommentsEnabled { get; set; }
        public bool? AutoApproveComments { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
    }

    // Request to update a post (all fields optional)
    public class UpdatePostRequestDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(220)]
        public string? Slug { get; set; }

        [MaxLength(400)]
        public string? Excerpt { get; set; }

        public string? ContentHtml { get; set; }
        public string? ContentMarkdown { get; set; }

        /// <summary> optional; "Public" | "Private" | "Restricted" </summary>
        public string? Visibility { get; set; }

        public IEnumerable<string>? TagNames { get; set; }
        public IEnumerable<string>? CategoryNames { get; set; }
        public IEnumerable<Guid>? AllowedUserIds { get; set; }

        public bool? CommentsEnabled { get; set; }
        public bool? AutoApproveComments { get; set; }

        /// <summary> optional; "Draft" | "Published" | "Archived" (usually use /publish endpoint for publish) </summary>
        public string? Status { get; set; }

        public string CoverImageUrl { get; set; } = string.Empty;
    }

    // Response DTOs
    public record AuthorSummaryDto(Guid Id, string Username, string? DisplayName, string? AvatarUrl);

    public record PostSummaryDto(
        Guid Id,
        string Title,
        string Slug,
        string? Excerpt,
        string? CoverImageUrl,
        string Status,
        string Visibility,
        bool IsRejected,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        AuthorSummaryDto Author,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Categories,
        int LikesCount
    );

    public record PostDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Excerpt,
        string ContentHtml,
        string? ContentMarkdown,
        string? CoverImageUrl,
        string Status,
        string Visibility,
        bool CommentsEnabled,
        bool AutoApproveComments,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        AuthorSummaryDto Author,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Categories,
        IReadOnlyList<Guid> AllowedAudienceUserIds,
        int LikesCount
    );

    public record PagedResponseDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

}
