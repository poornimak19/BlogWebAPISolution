using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Models
{
    public class User
    {

        public Guid Id { get; set; }

        [Required, MaxLength(64)]
        public string Username { get; set; } = default!;

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = default!;


        /// <summary>
        /// Hashed password bytes (e.g., HMACSHA512 hash of the plaintext password)
        /// </summary>
        [Required]
        public byte[] Password { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Hash key / salt bytes used to compute the hash above
        /// </summary>
        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();


        [MaxLength(128)]
        public string? DisplayName { get; set; }

        [MaxLength(1024)]
        public string? Bio { get; set; }

        [MaxLength(512)]
        public string? AvatarUrl { get; set; }

        public UserRole Role { get; set; } = UserRole.Reader;

        [MaxLength(16)]
        public string Status { get; set; } = "active"; // active | suspended

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Follow> Following { get; set; } = new List<Follow>();
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
        public ICollection<UserInterest> Interests { get; set; } = new List<UserInterest>();

        // For Restricted visibility allow-list
        public ICollection<PostAudience> AllowedPostAudiences { get; set; } = new List<PostAudience>();

        public UserSettings? Settings { get; set; }

        public bool IsSuspended { get; set; } = false;
        public bool CanComment { get; set; } = true;

        // Premium subscription
        public bool IsPremiumSubscriber { get; set; } = false;
        public DateTime? PremiumExpiresAt { get; set; }
        public ICollection<PremiumReadLog> PremiumReadLogs { get; set; } = new List<PremiumReadLog>();
    }
}
