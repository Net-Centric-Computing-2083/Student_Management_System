using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Internal marks are required.")]
        [Range(0, 20)]
        [Display(Name = "Internal Marks")]
        public decimal InternalMarks { get; set; }

        [Required(ErrorMessage = "Practical marks are required.")]
        [Range(0, 20)]
        [Display(Name = "Practical Marks")]
        public decimal PracticalMarks { get; set; }

        [Required(ErrorMessage = "Final marks are required.")]
        [Range(0, 60)]
        [Display(Name = "Final Marks")]
        public decimal FinalMarks { get; set; }

        [Display(Name = "Total Marks")]
        public decimal TotalMarks { get; set; }

        [StringLength(2)]
        public string Grade { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Result Date")]
        public DateTime ResultDate { get; set; } = DateTime.Today;

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [NotMapped]
        public decimal GPA
        {
            get
            {
                return Grade switch
                {
                    "A+" => 4.0m,
                    "A" => 3.6m,
                    "B+" => 3.2m,
                    "B" => 2.8m,
                    "C+" => 2.4m,
                    "C" => 2.0m,
                    "D" => 1.6m,
                    _ => 0.0m
                };
            }
        }
    }
}