using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models.DTOs
{


    public record TagDto(
        int Id,
        string Name,
        string Slug
    );

    public record CategoryDto(
        int Id,
        string Name,
        string Slug
    );

    public class CreateTagRequestDto
    {
        [Required, MaxLength(64)]
        public string Name { get; set; } = default!;
    }

    public class CreateCategoryRequestDto
    {
        [Required, MaxLength(64)]
        public string Name { get; set; } = default!;
    }

}
