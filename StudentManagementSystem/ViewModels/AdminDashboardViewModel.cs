namespace StudentManagementSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }

        public int TotalTeachers { get; set; }

        public int TotalDepartments { get; set; }

        public int TotalCourses { get; set; }

        public int TotalEnrollments { get; set; }

        public int TotalResults { get; set; }

        public int TotalAttendance { get; set; }

        public int PendingLeaveRequests { get; set; }

        public int NewContactMessages { get; set; }

        public List<AdminActivityItem> RecentActivity { get; set; }
            = new List<AdminActivityItem>();
    }

    public class AdminActivityItem
    {
        public DateTime When { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
