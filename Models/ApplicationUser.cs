using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models
{
    public enum UserRole { Admin, Teacher, Student }

    public class ApplicationUser
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Number of consecutive failed login attempts.</summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>UTC time when account lock expires (null = not locked).</summary>
        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }
}
