using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models
{
    public class Category
    {

        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(80)]
        public string Slug { get; set; } = default!;

        public ICollection<PostCategory> PostCategories { get; set; } = new List<PostCategory>();

    }
}
