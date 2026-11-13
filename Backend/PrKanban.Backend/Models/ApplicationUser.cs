using Microsoft.AspNetCore.Identity;

namespace PrKanban.Models
{
    /// <summary>
    /// Extended user model inheriting from IdentityUser
    /// Additional fields beyond default Identity model
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's full display name
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// URL to user's profile picture
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// UTC timestamp when user created their account
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp of last profile update
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp of last successful login
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        /// <summary>
        /// User's board configuration (1:1 relationship)
        /// </summary>
        public virtual BoardConfig? BoardConfig { get; set; }

        /// <summary>
        /// Collection of tasks owned by this user (1:many relationship)
        /// </summary>
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
