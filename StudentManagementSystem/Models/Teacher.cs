using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Teacher name is required.")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Teacher email is required.")]
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public Department? Department { get; set; }
    }
}
