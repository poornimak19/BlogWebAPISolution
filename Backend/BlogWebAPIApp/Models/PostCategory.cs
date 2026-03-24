namespace BlogWebAPIApp.Models
{
    public class PostCategory
    {

        public Guid PostId { get; set; }
        public Post Post { get; set; } = default!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = default!;

    }
}
