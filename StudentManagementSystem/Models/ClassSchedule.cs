using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class ClassSchedule
    {
        [Key]
        public int ClassScheduleId { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        public Course? Course { get; set; }

        [Required(ErrorMessage = "Teacher is required.")]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; }

        public Teacher? Teacher { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }

        public Semester? Semester { get; set; }

        [Required(ErrorMessage = "Day is required.")]
        [StringLength(10)]
        [Display(Name = "Day")]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Room is required.")]
        [StringLength(50)]
        public string Room { get; set; } = string.Empty;
    }
}
