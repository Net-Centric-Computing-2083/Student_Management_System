namespace StudentManagementSystem.Models
{
    public class AttendanceMarkingViewModel
    {
        public int CourseId { get; set; }
        public int SemesterId { get; set; }
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        public List<StudentAttendanceViewModel> Students { get; set; } = new();
    }

    public class StudentAttendanceViewModel
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public bool IsPresent { get; set; }
    }
}
