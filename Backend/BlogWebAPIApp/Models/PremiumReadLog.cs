namespace BlogWebAPIApp.Models
{
    public class PremiumReadLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
