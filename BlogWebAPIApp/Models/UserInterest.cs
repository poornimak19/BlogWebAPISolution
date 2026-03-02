namespace BlogWebAPIApp.Models
{
    public class UserInterest
    {

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = default!;

        public double Weight { get; set; } = 1.0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
