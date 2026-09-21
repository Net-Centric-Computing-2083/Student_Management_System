using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class TeacherStudentPerformanceViewModel
    {
        public Student Student { get; set; } = null!;

        public decimal AttendancePercentage { get; set; }

        public decimal AverageGPA { get; set; }

        public int AssignmentsSubmitted { get; set; }

        public int TotalAssignments { get; set; }
    }

    public class TeacherCourseReportViewModel
    {
        public Course Course { get; set; } = null!;

        public int EnrolledStudents { get; set; }

        public decimal AttendancePercentage { get; set; }

        public decimal AverageGPA { get; set; }

        public int TotalAssignments { get; set; }

        public int TotalSubmissions { get; set; }
    }
}
