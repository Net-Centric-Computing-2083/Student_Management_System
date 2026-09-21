using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class CourseMaterial
    {
        [Key]
        public int CourseMaterialId { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        public int CourseId { get; set; }

        public Course? Course { get; set; }

        [Required(ErrorMessage = "Teacher is required.")]
        public int TeacherId { get; set; }

        public Teacher? Teacher { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "File")]
        public string FilePath { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Uploaded Date")]
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
