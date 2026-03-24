namespace BlogWebAPIApp.Models
{
    public class CommentLike
    {

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public Guid CommentId { get; set; }
        public Comment Comment { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
