using System.Collections.Generic;

namespace StudentManagementSystem.Models
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }

        public List<Student> RecentStudents { get; set; }
            = new List<Student>();
    }
}   