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

        [Required(ErrorMessage = "Department code is required.")]
        [StringLength(10,
            ErrorMessage = "Department code cannot exceed 10 characters.")]
        [Display(Name = "Department Code")]
        public string? DepartmentCode { get; set; }

        [Required(ErrorMessage = "Head of Department is required.")]
        [StringLength(100,
            ErrorMessage = "HOD name cannot exceed 100 characters.")]
        [Display(Name = "Head of Department")]
        public string? HodName { get; set; }

        [StringLength(500,
            ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        // Navigation property: one department can have many students
        public ICollection<Student> Students { get; set; } = new List<Student>();

        // Navigation property: one department can have many courses
        public ICollection<Course> Courses { get; set; } = new List<Course>();

        // Navigation property: one department can have many teachers
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    }
}