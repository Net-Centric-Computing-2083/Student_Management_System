using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required]
        [StringLength(200)]
        public string? Action { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Action Time")]
        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}