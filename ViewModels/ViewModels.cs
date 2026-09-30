using System.ComponentModel.DataAnnotations;
using SchoolManagement.Models;

namespace SchoolManagement.ViewModels
{
    // ── Auth ──────────────────────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    // ── Account Management ────────────────────────────────────────────────────

    public class CreateUserViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; }
    }

    // ── Class Management ──────────────────────────────────────────────────────

    public class CreateClassViewModel
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class ClassDetailViewModel
    {
        public SchoolManagement.Models.Class Class { get; set; } = null!;
        public List<ApplicationUser> Teachers { get; set; } = new();
        public List<ApplicationUser> Students { get; set; } = new();
        public List<Lesson> Lessons { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
    }

    public class AddMemberViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public List<ApplicationUser> AvailableUsers { get; set; } = new();
        public int SelectedUserId { get; set; }
        public string MemberType { get; set; } = "Student"; // "Teacher" | "Student"
    }

    // ── Lesson ────────────────────────────────────────────────────────────────

    public class CreateLessonViewModel
    {
        public int ClassId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    // ── Assignment ────────────────────────────────────────────────────────────

    public class CreateAssignmentViewModel
    {
        public int ClassId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, DataType(DataType.DateTime)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
    }

    public class SubmitAssignmentViewModel
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public string AssignmentDescription { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Your Submission")]
        public string SubmissionContent { get; set; } = string.Empty;
    }

    public class GradeSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SubmissionContent { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }

        [Required, Range(0, 100)]
        public int Grade { get; set; }

        [MaxLength(1000)]
        public string? Feedback { get; set; }
    }
}
