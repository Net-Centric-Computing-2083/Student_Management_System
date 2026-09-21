using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Semester
    {
        [Key]
        public int SemesterId { get; set; }

        [Required]
        [Display(Name = "Academic Year")]
        public string? AcademicYear { get; set; }

        [Required]
        [Display(Name = "Semester")]
        public string? SemesterName { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();
    }
}