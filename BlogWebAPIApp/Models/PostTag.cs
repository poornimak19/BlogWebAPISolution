namespace BlogWebAPIApp.Models
{
    public class PostTag
    {

        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = default!;

    }
}
