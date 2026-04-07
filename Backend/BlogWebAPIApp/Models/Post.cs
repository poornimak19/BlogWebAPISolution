using System.ComponentModel.DataAnnotations;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models
{
   
    public class Post
    {

        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }
        public User Author { get; set; } = default!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required, MaxLength(220)]
        public string Slug { get; set; } = default!; // unique

        [MaxLength(400)]
        public string? Excerpt { get; set; }

        // Rich text; choose HTML (from Angular editor) or Markdown
        public string ContentHtml { get; set; } = string.Empty;
        public string? ContentMarkdown { get; set; }

        [MaxLength(512)]
        public string? CoverImageUrl { get; set; }

        [MaxLength(512)]
        public string? AudioUrl { get; set; }

        [MaxLength(512)]
        public string? VideoUrl { get; set; }

        public bool IsPremium { get; set; } = false;

        public PostStatus Status { get; set; } = PostStatus.Draft;
        public Visibility Visibility { get; set; } = Visibility.Public;

        public bool CommentsEnabled { get; set; } = true;
        public bool AutoApproveComments { get; set; } = true;
        public bool IsRejected { get; set; } = false;

        // Scheduling removed
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public ICollection<PostCategory> PostCategories { get; set; } = new List<PostCategory>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
        public ICollection<PostAudience> Audience { get; set; } = new List<PostAudience>();

    }
}
