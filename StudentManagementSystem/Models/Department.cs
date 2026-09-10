using Microsoft.Identity.Client.NativeInterop;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Department name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Department name must be between 2 and 100 characters.")]
        [Display(Name = "Department Name")]
        public string? DepartmentName { get; set; }

        [StringLength(500,
            ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        // Navigation property: one department can have many students
        public ICollection<Student> Students { get; set; } = new List<Student>();

        // Navigation property: one department can have many courses
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}