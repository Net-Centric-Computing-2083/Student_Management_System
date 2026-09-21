using System.ComponentModel.DataAnnotations;
using Student_Management_System.Models;

namespace StudentManagementSystem.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        [StringLength(500)]
        public string? Message { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Read")]
        public bool IsRead { get; set; } = false;

        // ==================================================
        // TARGETING (added for Student/Teacher/Admin modules)
        // ==================================================

        // The Identity user this notification belongs to.
        // Null = legacy/system-wide notification (kept for
        // backward compatibility with any existing rows).
        [Display(Name = "Recipient")]
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        // Category used for icons/filtering, e.g. "Assignment",
        // "Attendance", "Result", "Announcement", "Leave".
        [StringLength(50)]
        public string? Type { get; set; }

        // Optional relative link (controller/action/id) the
        // notification should navigate to when clicked.
        [StringLength(200)]
        public string? Link { get; set; }
    }
}