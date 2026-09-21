using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class HomeViewModel
    {
        public int TotalStudents { get; set; }

        public List<Student> RecentStudents { get; set; } = new();

        public LoginViewModel Login { get; set; } = new();
    }
}