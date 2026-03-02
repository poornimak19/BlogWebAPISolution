using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models
{
    public class Tag
    {

        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(80)]
        public string Slug { get; set; } = default!;

        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();

    }
}
