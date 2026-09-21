using Microsoft.AspNetCore.Identity;

namespace Student_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Links the Identity account to the Student record
        public int? StudentId { get; set; }

        public StudentManagementSystem.Models.Student? Student { get; set; }

        // Links the Identity account to the Teacher record
        public int? TeacherId { get; set; }

        public StudentManagementSystem.Models.Teacher? Teacher { get; set; }
    }
}