using System;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models.DTOs
{
    public class UserAdminDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string Role { get; set; } = default!;
        public bool IsSuspended { get; set; }
        public bool CanComment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChangeRoleDto
    {
        public UserRole Role { get; set; }
    }

    public class SuspendUserDto
    {
        public bool Suspend { get; set; }
    }

    public class CommentBanDto
    {
        public bool CanComment { get; set; }
    }

    public class RenameDto
    {
        public string Name { get; set; } = default!;
    }
}