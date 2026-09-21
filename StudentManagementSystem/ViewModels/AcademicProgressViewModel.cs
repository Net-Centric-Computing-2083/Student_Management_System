using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class AcademicProgressViewModel
    {
        public Student Student { get; set; } = null!;

        public List<AcademicProgressItemViewModel> Courses { get; set; }
            = new List<AcademicProgressItemViewModel>();

        public decimal OverallGPA { get; set; }

        public decimal AverageMarks { get; set; }

        public int TotalCourses { get; set; }

        public int PassedCourses { get; set; }
    }

    public class AcademicProgressItemViewModel
    {
        public string CourseName { get; set; } = string.Empty;

        public string CourseCode { get; set; } = string.Empty;

        public int CreditHours { get; set; }

        public string SemesterName { get; set; } = string.Empty;

        public string AcademicYear { get; set; } = string.Empty;

        public decimal TotalMarks { get; set; }

        public string Grade { get; set; } = string.Empty;

        public decimal GPA { get; set; }

        public bool HasResult { get; set; }
    }
}