using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }

    public class AssignmentSubmission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public int StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        [Required]
        public string SubmissionContent { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Nullable until graded.</summary>
        public int? Grade { get; set; }

        [MaxLength(1000)]
        public string? Feedback { get; set; }

        public DateTime? GradedAt { get; set; }
        public int? GradedByTeacherId { get; set; }
        public ApplicationUser? GradedBy { get; set; }
    }
}
