using System.ComponentModel.DataAnnotations;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models.DTOs
{

    public record RegisterRequestDto(
            [Required, EmailAddress] string Email,
            [Required, MinLength(3), MaxLength(64)] string Username,
            [Required, MinLength(6)] string Password,
            string? DisplayName,
            UserRole Role

        );

    public record LoginRequestDto(
        [Required] string EmailOrUsername,
        [Required] string Password
    );

    public record AuthResponseDto(
        string AccessToken
        //string Username,
        //string Role
    );

    public record MeResponseDto(
        Guid Id,
        string Username,
        string? DisplayName,
        string Email,
        string Role,
        bool IsPremiumSubscriber,
        DateTime? PremiumExpiresAt
    );

}
