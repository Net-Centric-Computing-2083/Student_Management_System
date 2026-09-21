using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required(ErrorMessage = "Assignment title is required.")]
        [StringLength(150)]
        [Display(Name = "Assignment Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Assignment description is required.")]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course is required.")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Teacher is required.")]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Created Date")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties

        public Course? Course { get; set; }

        public Teacher? Teacher { get; set; }

        public ICollection<AssignmentSubmission> Submissions { get; set; }
            = new List<AssignmentSubmission>();
    }
}