using Student_Management_System.Models;

namespace StudentManagementSystem.ViewModels
{
    public class AdminUserRowViewModel
    {
        public ApplicationUser User { get; set; } = null!;

        public IList<string> Roles { get; set; } = new List<string>();

        public bool IsLockedOut { get; set; }
    }

    public class AdminReportsViewModel
    {
        public int TotalStudents { get; set; }

        public int TotalTeachers { get; set; }

        public int TotalCourses { get; set; }

        public int TotalDepartments { get; set; }

        public int TotalEnrollments { get; set; }

        public decimal AverageGPA { get; set; }

        public decimal OverallAttendancePercentage { get; set; }

        public int TotalAssignments { get; set; }

        public int TotalSubmissions { get; set; }

        public int GradedSubmissions { get; set; }

        public decimal TotalFeesBilled { get; set; }

        public decimal TotalFeesCollected { get; set; }

        public int PendingLeaveRequests { get; set; }
    }

    public class SecurityOverviewViewModel
    {
        public int TotalUsers { get; set; }

        public int LockedOutUsers { get; set; }

        public int AdminCount { get; set; }

        public int TeacherCount { get; set; }

        public int StudentCount { get; set; }
    }
}
