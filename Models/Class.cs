using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    /// <summary>Join table: which teachers are assigned to which classes.</summary>
    public class ClassTeacher
    {
        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Join table: which students are enrolled in which classes.</summary>
    public class ClassStudent
    {
        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}
