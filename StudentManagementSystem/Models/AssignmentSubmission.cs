using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class AssignmentSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        [Required]
        [Display(Name = "Assignment")]
        public int AssignmentId { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Submission is required.")]
        [StringLength(5000)]
        [Display(Name = "Submission")]
        public string SubmissionText { get; set; } = string.Empty;

        [Display(Name = "Submitted Date")]
        [DataType(DataType.DateTime)]
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Submitted";

        // ==================================================
        // GRADING (added for Teacher grading workflow)
        // ==================================================

        [Range(0, 100, ErrorMessage = "Marks must be between 0 and 100.")]
        [Display(Name = "Marks")]
        public decimal? Marks { get; set; }

        [StringLength(1000)]
        [Display(Name = "Feedback")]
        public string? Feedback { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Graded Date")]
        public DateTime? GradedDate { get; set; }

        // Navigation properties

        public Assignment? Assignment { get; set; }

        public Student? Student { get; set; }
    }
}