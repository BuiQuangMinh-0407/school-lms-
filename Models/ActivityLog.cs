using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        /// <summary>Null when the login attempt uses an unknown username.</summary>
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;   // e.g. "LoginSuccess", "LoginFailed", "Logout"

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public bool Success { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
