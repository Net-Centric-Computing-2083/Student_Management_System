using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        // Optional: leave tied to a specific course, so that
        // course's Teacher can review it. Left null for general
        // leave, which goes to Admin.
        [Display(Name = "Course (optional)")]
        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        [CustomValidation(typeof(LeaveRequest), nameof(ValidateDateRange))]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [DataType(DataType.DateTime)]
        [Display(Name = "Requested Date")]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        // Set once reviewed, by either a Teacher or an Admin.
        [Display(Name = "Reviewed By")]
        public string? ReviewedByUserId { get; set; }

        [StringLength(100)]
        [Display(Name = "Reviewed By")]
        public string? ReviewedByName { get; set; }

        [StringLength(20)]
        [Display(Name = "Reviewer Role")]
        public string? ReviewedByRole { get; set; }

        [StringLength(500)]
        [Display(Name = "Response")]
        public string? ReviewComment { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedDate { get; set; }

        public static ValidationResult ValidateDateRange(
            DateTime endDate,
            ValidationContext context)
        {
            var instance = (LeaveRequest)context.ObjectInstance;

            if (endDate < instance.StartDate)
            {
                return new ValidationResult(
                    "End date cannot be before start date.");
            }

            return ValidationResult.Success!;
        }
    }
}
