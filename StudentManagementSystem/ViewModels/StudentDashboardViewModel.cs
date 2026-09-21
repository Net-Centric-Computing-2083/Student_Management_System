using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = null!;

        public int TotalCourses { get; set; }

        public decimal OverallGPA { get; set; }

        public decimal AttendancePercentage { get; set; }

        public int PendingAssignments { get; set; }

        public int PendingLeaveRequests { get; set; }

        public int UnreadNotifications { get; set; }

        public List<Assignment> UpcomingDeadlines { get; set; }
            = new List<Assignment>();

        public List<Announcement> RecentAnnouncements { get; set; }
            = new List<Announcement>();

        public List<Notification> RecentNotifications { get; set; }
            = new List<Notification>();
    }
}
