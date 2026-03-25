using System.ComponentModel.DataAnnotations;

namespace BlogWebAPIApp.Models.DTOs
{

    public record UserProfileDto(
            Guid Id,
            string Username,
            string? DisplayName,
            string? Bio,
            string? AvatarUrl,
            int Followers,
            int Following
        );

    public class UpdateUserProfileDto
    {
        [MaxLength(128)]
        public string? DisplayName { get; set; }

        [MaxLength(1024)]
        public string? Bio { get; set; }

        [MaxLength(512)]
        public string? AvatarUrl { get; set; }
    }

    public class UserSearchDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
