using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Course name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Course name must be between 2 and 100 characters.")]
        [Display(Name = "Course Name")]
        public string? CourseName { get; set; }

        [Required(ErrorMessage = "Course code is required.")]
        [StringLength(20, MinimumLength = 2,
            ErrorMessage = "Course code must be between 2 and 20 characters.")]
        [Display(Name = "Course Code")]
        public string? CourseCode { get; set; }

        [Required(ErrorMessage = "Credit hours are required.")]
        [Range(1, 10,
            ErrorMessage = "Credit hours must be between 1 and 10.")]
        [Display(Name = "Credit Hours")]
        public int CreditHours { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        // Navigation property
        public Department? Department { get; set; }

        // One course can have many enrollments
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}