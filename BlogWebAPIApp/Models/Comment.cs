using System.ComponentModel.DataAnnotations;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models
{
    public class Comment
    {

        public Guid Id { get; set; }

        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;

        public Guid? AuthorId { get; set; }
        public User? Author { get; set; }

        public Guid? ParentCommentId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        [Required, MaxLength(8000)]
        public string Content { get; set; } = default!; // sanitize if HTML

        public CommentStatus Status { get; set; } = CommentStatus.Approved;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();

    }
}
