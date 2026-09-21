using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class ContactMessage
    {
        [Key]
        public int ContactMessageId { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        [Required(ErrorMessage = "Teacher is required.")]
        public int TeacherId { get; set; }

        public Teacher? Teacher { get; set; }

        [Display(Name = "Course")]
        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000)]
        [Display(Name = "Message")]
        public string MessageText { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Sent Date")]
        public DateTime SentDate { get; set; } = DateTime.Now;

        // "Student" or "Teacher" — who sent this message.
        [Required]
        [StringLength(10)]
        public string SenderRole { get; set; } = "Student";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "New";

        // Self-referencing link so a teacher's reply threads
        // under the original student message.
        public int? ParentMessageId { get; set; }

        public ContactMessage? ParentMessage { get; set; }

        public ICollection<ContactMessage> Replies { get; set; }
            = new List<ContactMessage>();
    }
}
