using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>Only published lessons are visible to students.</summary>
        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
