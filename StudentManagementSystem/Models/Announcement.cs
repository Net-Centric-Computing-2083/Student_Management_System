using System.ComponentModel.DataAnnotations;
using Student_Management_System.Models;

namespace StudentManagementSystem.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(3000)]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Posted Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Identity user who authored the announcement
        // (Admin or Teacher).
        [Required]
        public string AuthorUserId { get; set; } = string.Empty;

        public ApplicationUser? Author { get; set; }

        [StringLength(100)]
        [Display(Name = "Posted By")]
        public string AuthorName { get; set; } = string.Empty;

        // "System" = admin-wide announcement visible to everyone.
        // "Course" = teacher/admin announcement scoped to one course.
        [Required]
        [StringLength(20)]
        public string Scope { get; set; } = "System";

        // Only set when Scope == "Course".
        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        // Optional role targeting for system-wide announcements:
        // "All", "Student", "Teacher".
        [StringLength(20)]
        [Display(Name = "Target Audience")]
        public string TargetRole { get; set; } = "All";
    }
}
