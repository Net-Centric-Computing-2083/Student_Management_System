using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Marks must be between 0 and 100.")]
        public decimal Marks { get; set; }

        [Required]
        [StringLength(2)]
        public string Grade { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime ResultDate { get; set; } = DateTime.Now;

        // Navigation property to the existing Student model
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        // Navigation property to the existing Course model
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}