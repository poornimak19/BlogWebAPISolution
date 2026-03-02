namespace BlogWebAPIApp.Models
{
    public class PostAudience
    {

        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

    }
}
