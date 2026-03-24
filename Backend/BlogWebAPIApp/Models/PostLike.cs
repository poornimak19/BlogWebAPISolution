namespace BlogWebAPIApp.Models
{
    public class PostLike
    {

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
